using System.Globalization;

namespace StockBacktest.Services;

public sealed class StooqProvider : IPriceProvider
{
    private readonly HttpClient _http;

    public StooqProvider(HttpClient http)
    {
        _http = http;
    }

    public string Name => "Stooq";

    public async Task<(decimal Price, DateOnly ActualDate)?> GetPriceAsync(
        string ticker, DateOnly date, CancellationToken ct = default)
    {
        // Stooq expects lowercase ticker, search 10-day window
        var stooqTicker = ticker.ToLowerInvariant();
        var d1 = date.ToString("yyyyMMdd");
        var d2 = date.AddDays(10).ToString("yyyyMMdd");

        var url = $"https://stooq.com/q/d/l/?s={Uri.EscapeDataString(stooqTicker)}&d1={d1}&d2={d2}&i=d";

        Console.Error.WriteLine($"[Stooq] GET {url}");

        using var response = await _http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"[Stooq] HTTP {(int)response.StatusCode} for {stooqTicker}");
            return null;
        }

        var csv = await response.Content.ReadAsStringAsync(ct);

        // CSV format: Date,Open,High,Low,Close,Volume
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2)
        {
            Console.Error.WriteLine($"[Stooq] No data rows for {stooqTicker}");
            return null;
        }

        // Skip header, take first valid data row
        for (var i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Trim().Split(',');
            if (parts.Length < 5)
            {
                continue;
            }

            if (!DateOnly.TryParseExact(parts[0], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var actualDate))
            {
                continue;
            }

            if (!decimal.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
            {
                continue;
            }

            Console.Error.WriteLine($"[Stooq] {stooqTicker} on {actualDate}: {price}");
            return (price, actualDate);
        }

        Console.Error.WriteLine($"[Stooq] No valid rows for {stooqTicker}");
        return null;
    }
}