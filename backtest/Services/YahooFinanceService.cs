using System.Text.Json;
using StockBacktest.Models;

namespace StockBacktest.Services;

public class YahooFinanceService
{
    private static readonly HttpClient Http;

    static YahooFinanceService()
    {
        Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        Http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<YahooQuote?> GetQuoteAsync(string ticker, CancellationToken ct = default)
    {
        var url =
            $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&range=5d";
        await Console.Error.WriteLineAsync($"[Yahoo] GetQuote {ticker}");

        using var doc = await FetchAsync(url, ct);
        if (doc is null)
        {
            return null;
        }

        var meta = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result")[0]
            .GetProperty("meta");

        return new YahooQuote
        {
            Currency = GetString(meta, "currency"),
            LongName = GetString(meta, "longName"),
            ShortName = GetString(meta, "shortName"),
            RegularMarketPrice = GetFloat(meta, "regularMarketPrice"),
            RegularMarketTime = GetUnixDateTime(meta, "regularMarketTime")
        };
    }

    public async Task<List<YahooRecord>> GetRecordsAsync(
        string ticker, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var period1 = ToUnix(from ?? DateTime.Today.AddDays(-7));
        var period2 = ToUnix((to ?? DateTime.Today).AddDays(1));
        var url =
            $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}?interval=1d&period1={period1}&period2={period2}";
        await Console.Error.WriteLineAsync($"[Yahoo] GetRecords {ticker} {from:yyyy-MM-dd} → {to:yyyy-MM-dd}");

        using var doc = await FetchAsync(url, ct);
        if (doc is null)
        {
            return [];
        }

        var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
        var timestamps = result.GetProperty("timestamp");
        var closes = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close");

        var records = new List<YahooRecord>();
        for (var i = 0; i < timestamps.GetArrayLength(); i++)
        {
            if (closes[i].ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            records.Add(new YahooRecord
            {
                Date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i].GetInt64()).DateTime,
                Close = Math.Round((decimal)closes[i].GetDouble(), 2)
            });
        }

        return records;
    }

    private static async Task<JsonDocument?> FetchAsync(string url, CancellationToken ct)
    {
        try
        {
            var json = await Http.GetStringAsync(url, ct);
            return JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Yahoo] Error: {ex.Message}");
            return null;
        }
    }

    private static string? GetString(JsonElement el, string key)
    {
        return el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    private static float? GetFloat(JsonElement el, string key)
    {
        return el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? (float)v.GetDouble() : null;
    }

    private static DateTime? GetUnixDateTime(JsonElement el, string key)
    {
        return el.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number
            ? DateTimeOffset.FromUnixTimeSeconds(v.GetInt64()).DateTime
            : null;
    }

    private static long ToUnix(DateTime dt)
    {
        return new DateTimeOffset(dt.ToUniversalTime()).ToUnixTimeSeconds();
    }
}