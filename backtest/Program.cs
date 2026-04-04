using System.Globalization;
using System.Text.Json;
using StockBacktest.Calculators;
using StockBacktest.Contracts;
using StockBacktest.Converters;
using StockBacktest.Models;
using StockBacktest.Services;
using StockBacktest.Strategies;

namespace StockBacktest;

internal static class Program
{
    /// <summary>
    ///     ishares core msci world (acc)
    ///     backtest EUNL.DE --buy 2010-01-03 --prices >EUNL.DE.json
    ///     backtest GERD.DE --buy 2010-01-03 --prices >GERD.DE.json
    /// </summary>
    /// <returns></returns>
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            await Console.Error.WriteLineAsync("Usage: backtest <ticker> [--buy yyyy-MM-dd] [--sell yyyy-MM-dd]");
            await Console.Error.WriteLineAsync("  <ticker>          Ticker symbol, e.g. SAP.DE or AAPL");
            await Console.Error.WriteLineAsync("  --buy yyyy-MM-dd  Buy date. Omit to get the latest known price.");
            await Console.Error.WriteLineAsync("  --sell yyyy-MM-dd Sell date. Defaults to today.");
            await Console.Error.WriteLineAsync("  --search <query>  Search for a ticker by company name.");
            await Console.Error.WriteLineAsync(
                "  --prices          Output full price series as JSON array for plotting.");
            await Console.Error.WriteLineAsync("  --simulate --in <prices.json> --strategy <name>  Run a strategy.");
            await Console.Error.WriteLineAsync("  --simulate --list  List available strategies.");
            return 0;
        }

        // Simple arg parsing: first non-flag arg = ticker, --key value pairs for the rest
        TickerTimeRange request;
        try
        {
            if (IsBacktestMode(args))
            {
                return await RunStrategyModeAsync(args);
            }

            var searchIndex = Array.IndexOf(args, "--search");
            if (searchIndex >= 0 && searchIndex + 1 < args.Length)
            {
                var ticker = args[searchIndex + 1];
                return await RunSearchModeAsync(ticker);
            }

            var pricesMode = args.Contains("--prices");
            request = CreateBackTestRequest(args);


            if (pricesMode)
            {
                return await RunFetchModeAsync(request);
            }

            var result = await RunBacktestAsync(request);
            Console.WriteLine(JsonSerializer.Serialize(result, BacktestJsonContext.Default.BacktestResult));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }

        return 0;
    }

    private static bool IsBacktestMode(string[] args)
    {
        return args.Contains("--simulate");
    }

    private static async Task<int> RunFetchModeAsync(TickerTimeRange request)
    {
        var yahooService = new YahooFinanceService();
        var records = await yahooService.GetRecordsAsync(request.Ticker, request.BuyDate, request.SellDate);
        var points = records
            .OrderBy(r => r.Date)
            .Select(r => new PricePoint { Date = r.Date, Price = r.Close ?? 0 })
            .ToArray();
        Console.WriteLine(JsonSerializer.Serialize(points, BacktestJsonContext.Default.PricePointArray));
        return 0;
    }

    private static async Task<int> RunSearchModeAsync(string query)
    {
        var yahoo = new YahooFinanceService();
        var results = await yahoo.SearchAsync(query);
        Console.WriteLine(JsonSerializer.Serialize(results.ToArray(),
            BacktestJsonContext.Default.YahooSearchResultArray));
        return 0;
    }

    private static TickerTimeRange CreateBackTestRequest(string[] args)
    {
        string? ticker = null;
        string? buyStr = null;
        string? sellStr = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--buy" when i + 1 < args.Length:
                    buyStr = args[++i];
                    break;
                case "--sell" when i + 1 < args.Length:
                    sellStr = args[++i];
                    break;
                default:
                    if (!args[i].StartsWith("--"))
                    {
                        ticker = args[i];
                    }

                    break;
            }
        }

        if (ticker is null)
        {
            throw new ArgumentException(
                "Missing ticker symbol. Usage: backtest <ticker> [--buy yyyy-MM-dd] [--sell yyyy-MM-dd]");
        }

        DateTime? buyDate = null;
        if (buyStr is not null)
        {
            if (!TryParseDate(buyStr, out var bd))
            {
                throw new ArgumentException($"Invalid --buy format: '{buyStr}'. Use yyyy-MM-dd");
            }

            buyDate = bd;
        }

        var sellDate = DateTime.Now;
        if (sellStr is not null)
        {
            if (!TryParseDate(sellStr, out var sd))
            {
                throw new ArgumentException($"Invalid --sell format: '{sellStr}'. Use yyyy-MM-dd");
            }

            sellDate = sd;
        }

        var request = new TickerTimeRange
        {
            Ticker = ticker,
            BuyDate = buyDate,
            SellDate = sellDate
        };
        return request;
    }

    private static async Task<BacktestResult> RunBacktestAsync(
        TickerTimeRange request)
    {
        var yahoo = new YahooFinanceService();
        var quote = await yahoo.GetQuoteAsync(request.Ticker);

        var result = new BacktestResult
        {
            Ticker = request.Ticker,
            Currency = quote.Currency,
            Name = quote.LongName ?? quote.ShortName
        };


        if (request.BuyDate is null)
        {
            var price = quote.RegularMarketPrice;
            result.BuyDate = quote.RegularMarketTime;

            if (price.HasValue)
            {
                result.BuyPrice = (decimal)price.Value;
            }
        }
        else
        {
            var tmp = await yahoo.GetRecordsAsync(request.Ticker, request.BuyDate, request.SellDate,
                CancellationToken.None);
            var records = tmp.OrderBy(r => r.Date).ToList();

            var buy = records.FirstOrDefault();
            var sell = records.LastOrDefault();

            result.BuyDate = buy?.Date;
            result.BuyPrice = buy?.Close;
            result.SellDate = sell?.Date;
            result.SellPrice = sell?.Close;


            if (result.HasBuyAndSell())
            {
                // Append return
                var (absolute, percent, annualized, holdingDays) = ReturnCalculator.Calculate(
                    result.BuyPrice!.Value,
                    result.SellPrice!.Value,
                    result.BuyDate!.Value,
                    result.SellDate!.Value);

                result.AbsoluteReturn = absolute;
                result.PercentReturn = percent;
                result.AnnualizedReturn = annualized;
                result.HoldingDays = holdingDays;
            }
        }

        return result;
    }

    private static async Task<int> RunStrategyModeAsync(string[] args)
    {
        // --simulate --list
        if (args.Contains("--list"))
        {
            var names = StrategyLoader.ListAvailable().ToList();
            if (names.Count == 0)
            {
                await Console.Error.WriteLineAsync(
                    "No strategies found");
            }
            else
            {
                foreach (var n in names)
                {
                    Console.WriteLine(n);
                }
            }

            return 0;
        }

        var inIndex = Array.IndexOf(args, "--in");
        var strategyIndex = Array.IndexOf(args, "--strategy");

        if (inIndex < 0 || inIndex + 1 >= args.Length)
        {
            throw new ArgumentException("--simulate requires --in <prices.json>");
        }

        if (strategyIndex < 0 || strategyIndex + 1 >= args.Length)
        {
            throw new ArgumentException("--simulate requires --strategy <name>");
        }

        var inputFile = args[inIndex + 1];
        var strategyName = args[strategyIndex + 1];

        var json = await File.ReadAllTextAsync(inputFile);
        var prices = JsonSerializer.Deserialize(json, BacktestJsonContext.Default.PricePointArray)
                     ?? throw new InvalidDataException("Could not parse price points from input file.");

        await Console.Error.WriteAsync(strategyName);
        var strategy = StrategyLoader.Find(strategyName);
        if (strategy is null)
        {
            var available = StrategyLoader.ListAvailable().ToList();
            var hint = available.Count > 0
                ? $" Available: {string.Join(", ", available)}"
                : " No strategy found.";
            await Console.Error.WriteLineAsync($"Strategy '{strategyName}' not found.{hint}");
            return 1;
        }

        var result = BacktestStrategyRunner.Run(strategy, prices);
        Console.WriteLine(JsonSerializer.Serialize(result, BacktestJsonContext.Default.SimulationResult));
        return 0;
    }

    private static bool TryParseDate(string? input, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return DateTime.TryParseExact(input.Trim(), "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}