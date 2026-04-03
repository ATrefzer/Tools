using System.Text.Json;

namespace StockBacktest.Services;

public sealed class YahooFinanceProvider : IPriceProvider
{
    private readonly HttpClient _http;

    public YahooFinanceProvider(HttpClient http)
    {
        _http = http;
    }

    public string Name => "YahooFinance";

    public async Task<(decimal Price, DateOnly ActualDate)?> GetPriceAsync(
        string ticker, DateOnly date, CancellationToken ct = default)
    {
        // Fetch a 10-day window starting at the requested date to catch non-trading days
        var period1 = ToUnixTimestamp(date);
        var period2 = ToUnixTimestamp(date.AddDays(10));

        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(ticker)}" +
                  $"?interval=1d&period1={period1}&period2={period2}";

        Console.Error.WriteLine($"[Yahoo] GET {url}");

        using var response = await _http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"[Yahoo] HTTP {(int)response.StatusCode} for {ticker}");
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var result = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result");

        if (result.ValueKind == JsonValueKind.Null || result.GetArrayLength() == 0)
        {
            Console.Error.WriteLine($"[Yahoo] No results for {ticker}");
            return null;
        }

        var first = result[0];

        var timestamps = first.GetProperty("timestamp").EnumerateArray().ToList();
        var closes = first
            .GetProperty("indicators")
            .GetProperty("quote")[0]
            .GetProperty("close")
            .EnumerateArray()
            .ToList();

        if (timestamps.Count == 0 || closes.Count == 0)
        {
            Console.Error.WriteLine($"[Yahoo] Empty data for {ticker}");
            return null;
        }

        // Find the first entry with a valid close price
        for (var i = 0; i < Math.Min(timestamps.Count, closes.Count); i++)
        {
            if (closes[i].ValueKind == JsonValueKind.Null)
                continue;

            var ts = timestamps[i].GetInt64();
            var actualDate = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime);
            var price = Math.Round((decimal)closes[i].GetDouble(), 2);

            Console.Error.WriteLine($"[Yahoo] {ticker} on {actualDate}: {price}");
            return (price, actualDate);
        }

        Console.Error.WriteLine($"[Yahoo] No valid close prices found for {ticker}");
        return null;
    }

    private static long ToUnixTimestamp(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeSeconds();
    }
}