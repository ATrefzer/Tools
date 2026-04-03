using System.Globalization;
using System.Text.Json;
using Finance.Net.Extensions;
using Finance.Net.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StockBacktest.Calculators;
using StockBacktest.Converters;
using StockBacktest.Models;

namespace StockBacktest;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h")
        {
            await Console.Error.WriteLineAsync("Usage: backtest <ticker> [--buy yyyy-MM-dd] [--sell yyyy-MM-dd]");
            await Console.Error.WriteLineAsync("  <ticker>          Ticker symbol, e.g. SAP.DE or AAPL");
            await Console.Error.WriteLineAsync("  --buy yyyy-MM-dd  Buy date. Omit to get the latest known price.");
            await Console.Error.WriteLineAsync("  --sell yyyy-MM-dd Sell date. Defaults to today.");
            return 0;
        }

        // Simple arg parsing: first non-flag arg = ticker, --key value pairs for the rest
        BacktestRequest request;
        try
        {
            request = CreateBackTestRequest(args);


            var services = new ServiceCollection();
            services.AddFinanceNet();
            var yahoo = services.BuildServiceProvider().GetRequiredService<IYahooFinanceService>();

            var result = await RunBacktestAsync(request, yahoo);
            Console.WriteLine(JsonSerializer.Serialize(result, BacktestJsonContext.Default.BacktestResult));
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }

        return 0;
    }

    private static BacktestRequest CreateBackTestRequest(string[] args)
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
                    else
                    {
                        throw new ArgumentException($"Unknown option: '{args[i]}'");
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

        var request = new BacktestRequest
        {
            Ticker = ticker,
            BuyDate = buyDate,
            SellDate = sellDate
        };
        return request;
    }

    private static async Task<BacktestResult> RunBacktestAsync(
        BacktestRequest request, IYahooFinanceService yahoo)
    {
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