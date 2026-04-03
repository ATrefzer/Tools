using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockBacktest.Calculators;
using StockBacktest.Models;
using StockBacktest.Services;

namespace StockBacktest;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            WriteIndented = true,
            Converters = { new DateOnlyJsonConverter(), new DateTimeJsonConverter() }
        };

        // Shared HttpClient
        var handler = new HttpClientHandler();
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        // CLI definition 
        var identifierOption = new Option<string>(
                "--identifier",
                "Stock identifier (ISIN, WKN or Ticker symbol)")
            { IsRequired = true };

        var typeOption = new Option<IdentifierType>(
            "--type",
            () => IdentifierType.ISIN,
            "Identifier type: ISIN | WKN | Ticker");

        var buyOption = new Option<string>(
                "--buy",
                "Buy date (yyyy-MM-dd)")
            { IsRequired = true };

        var sellOption = new Option<string?>(
            "--sell",
            () => null,
            "Sell date (yyyy-MM-dd). Defaults to today.");

        var rootCommand = new RootCommand("Stock backtesting tool — outputs JSON to stdout")
        {
            identifierOption,
            typeOption,
            buyOption,
            sellOption
        };

        rootCommand.SetHandler(async ctx =>
        {
            var identifier = ctx.ParseResult.GetValueForOption(identifierOption)!;
            var type = ctx.ParseResult.GetValueForOption(typeOption);
            var buyStr = ctx.ParseResult.GetValueForOption(buyOption)!;
            var sellStr = ctx.ParseResult.GetValueForOption(sellOption);

            if (!TryParseDate(buyStr, out var buyDate))
            {
                await WriteErrorAsync($"Invalid --buy format: '{buyStr}'. Use yyyy-MM-dd", jsonOptions);
                ctx.ExitCode = 1;
                return;
            }

            DateOnly? sellDate = null;
            if (sellStr is not null)
            {
                if (!TryParseDate(sellStr, out var sd))
                {
                    await WriteErrorAsync($"Invalid --sell format: '{sellStr}'. Use yyyy-MM-dd", jsonOptions);
                    ctx.ExitCode = 1;
                    return;
                }

                sellDate = sd;
            }

            var request = new BacktestRequest
            {
                Identifier = identifier,
                Type = type,
                BuyDate = buyDate,
                SellDate = sellDate
            };

            var result = await RunBacktestAsync(request, http);
            Console.WriteLine(JsonSerializer.Serialize(result, jsonOptions));
            ctx.ExitCode = result.Errors.Count > 0 && result.BuyPrice is null ? 1 : 0;
        });

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<BacktestResult> RunBacktestAsync(BacktestRequest request, HttpClient http)
    {
        var errors = new List<string>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        string? resolvedTicker = null;
        string? resolvedName = null;

        // 1. Resolve ticker if needed
        if (request.Type == IdentifierType.Ticker)
        {
            resolvedTicker = request.Identifier;
        }
        else
        {
            Console.Error.WriteLine($"Resolving {request.Type} {request.Identifier}...");

            var openFigi = new OpenFigiResolver(http);
            var resolved = await openFigi.ResolveAsync(request.Identifier, request.Type);

            if (resolved is null)
            {
                Console.Error.WriteLine("OpenFIGI returned nothing, trying Yahoo search...");
                var yahooSearch = new YahooSearchResolver(http);
                resolved = await yahooSearch.ResolveAsync(request.Identifier, request.Type);
            }

            if (resolved is null)
            {
                errors.Add(
                    $"Symbol could not be resolved: no results from OpenFIGI or Yahoo Search for '{request.Identifier}'");
                return new BacktestResult
                {
                    Identifier = request.Identifier,
                    IdentifierType = request.Type.ToString(),
                    Errors = errors
                };
            }

            resolvedTicker = resolved.Ticker;
            resolvedName = resolved.Name;
        }

        // 2. Determine sell date
        var sellDate = request.SellDate ?? today;
        var isSellToday = sellDate == today;

        if (request.BuyDate >= sellDate)
        {
            errors.Add($"Buy date ({request.BuyDate}) must be before sell date ({sellDate})");
            return new BacktestResult
            {
                Identifier = request.Identifier,
                IdentifierType = request.Type.ToString(),
                ResolvedTicker = resolvedTicker,
                ResolvedName = resolvedName,
                Errors = errors
            };
        }

        // 3. Fetch prices — try Yahoo first, fall back to Stooq
        var yahoo = new YahooFinanceProvider(http);
        var stooq = new StooqProvider(http);

        var buyResult = await FetchWithFallback(yahoo, stooq, resolvedTicker, request.BuyDate);
        var sellResult = await FetchWithFallback(yahoo, stooq, resolvedTicker, sellDate);

        if (buyResult is null)
            errors.Add($"Could not fetch buy price for '{resolvedTicker}' around {request.BuyDate}");
        if (sellResult is null)
            errors.Add($"Could not fetch sell price for '{resolvedTicker}' around {sellDate}");

        if (buyResult is null || sellResult is null)
            return new BacktestResult
            {
                Identifier = request.Identifier,
                IdentifierType = request.Type.ToString(),
                ResolvedTicker = resolvedTicker,
                ResolvedName = resolvedName,
                Errors = errors
            };

        var (buyPrice, actualBuyDate, buySource) = buyResult.Value;
        var (sellPrice, actualSellDate, sellSource) = sellResult.Value;
        var dataSource = buySource == sellSource ? buySource : $"{buySource}/{sellSource}";

        // 4. Calculate returns
        var (absolute, percent, annualized, holdingDays) = ReturnCalculator.Calculate(
            buyPrice, sellPrice, actualBuyDate, actualSellDate);

        // 5. Fetch currency from Yahoo meta (best effort)
        var currency = await TryGetCurrencyAsync(http, resolvedTicker);

        return new BacktestResult
        {
            Identifier = request.Identifier,
            IdentifierType = request.Type.ToString(),
            ResolvedTicker = resolvedTicker,
            ResolvedName = resolvedName,
            Currency = currency,
            BuyDate = actualBuyDate,
            BuyPrice = buyPrice,
            SellDate = actualSellDate,
            SellPrice = sellPrice,
            IsSellDateToday = isSellToday && actualSellDate == today,
            AbsoluteReturn = absolute,
            PercentReturn = percent,
            AnnualizedReturn = annualized,
            HoldingDays = holdingDays,
            DataSource = dataSource,
            CalculatedAt = DateTime.UtcNow,
            Errors = errors
        };
    }

    private static async Task<(decimal Price, DateOnly Date, string Source)?> FetchWithFallback(
        YahooFinanceProvider yahoo, StooqProvider stooq, string ticker, DateOnly date)
    {
        var yahooResult = await yahoo.GetPriceAsync(ticker, date);
        if (yahooResult.HasValue)
            return (yahooResult.Value.Price, yahooResult.Value.ActualDate, yahoo.Name);

        Console.Error.WriteLine($"Yahoo failed for {ticker} on {date}, trying Stooq...");
        var stooqResult = await stooq.GetPriceAsync(ticker, date);
        if (stooqResult.HasValue)
            return (stooqResult.Value.Price, stooqResult.Value.ActualDate, stooq.Name);

        return null;
    }

    private static async Task<string?> TryGetCurrencyAsync(HttpClient http, string ticker)
    {
        try
        {
            var url =
                $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=1d";
            using var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var meta = doc.RootElement.GetProperty("chart").GetProperty("result")[0].GetProperty("meta");
            return meta.TryGetProperty("currency", out var c) ? c.GetString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseDate(string? input, out DateOnly result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input)) return false;
        return DateOnly.TryParseExact(input.Trim(), "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private static async Task WriteErrorAsync(string message, JsonSerializerOptions opts)
    {
        var result = new BacktestResult { Errors = [message] };
        Console.WriteLine(JsonSerializer.Serialize(result, opts));
        await Task.CompletedTask;
    }
}