using System.Globalization;
using System.Text.Json;
using AngleSharp.Html.Dom;
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

        DateTime? sellDate = null;
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
        BacktestResult? result = null;

        var quote = await yahoo.GetQuoteAsync(request.Ticker);
        if (request.BuyDate is null)
        {
            result = new BacktestResult
            {
                Ticker = request.Ticker,
                Currency = quote.Currency,
                BuyDate = DateTime.Now,
                BuyPrice = (decimal?)quote.RegularMarketPrice.GetValueOrDefault(0.0),
            };
        }
        else
        {
            var tmp  = await yahoo.GetRecordsAsync(request.Ticker, request.BuyDate, request.SellDate,
                CancellationToken.None);
            var records = tmp.OrderBy(r => r.Date).ToList();

            // No buy date → return latest known price only
            
            var buy = records.FirstOrDefault();
            var sell = records.LastOrDefault();


            var buyPrice = buy?.Close;
            var buyDate = buy?.Date;
            var sellPrice = sell?.Close;
            var sellDate = sell?.Date;

            result = new BacktestResult
            {
                Ticker = request.Ticker,
                Currency = quote.Currency,
                BuyDate = buyDate,
                BuyPrice = buyPrice,
                SellDate = sellDate,
                SellPrice = sellPrice
            };


            if (buyPrice != null && sellPrice != null && sellDate != null && buyDate != null)
            {
                var (absolute, percent, annualized, holdingDays) = ReturnCalculator.Calculate(
                    buyPrice.Value, sellPrice.Value, buyDate.Value, sellDate.Value);

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