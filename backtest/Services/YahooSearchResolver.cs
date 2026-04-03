using System.Text.Json;
using StockBacktest.Models;

namespace StockBacktest.Services;

/// <summary>
///     Fallback resolver using Yahoo Finance's search endpoint.
///     Used when OpenFIGI returns no results.
/// </summary>
public sealed class YahooSearchResolver : ISymbolResolver
{
    private readonly HttpClient _http;

    public YahooSearchResolver(HttpClient http)
    {
        _http = http;
    }

    public async Task<ResolvedSymbol?> ResolveAsync(
        string identifier, IdentifierType type, CancellationToken ct = default)
    {
        if (type == IdentifierType.Ticker)
        {
            return null;
        }

        var url = $"https://query2.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(identifier)}";

        Console.Error.WriteLine($"[YahooSearch] GET {url}");

        using var response = await _http.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"[YahooSearch] HTTP {(int)response.StatusCode}");
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var quotes = doc.RootElement
            .GetProperty("quotes");

        foreach (var item in quotes.EnumerateArray())
        {
            if (!item.TryGetProperty("symbol", out var symEl))
            {
                continue;
            }

            var symbol = symEl.GetString();
            if (string.IsNullOrEmpty(symbol))
            {
                continue;
            }

            var name = item.TryGetProperty("shortname", out var snEl) ? snEl.GetString()
                : item.TryGetProperty("longname", out var lnEl) ? lnEl.GetString()
                : null;

            Console.Error.WriteLine($"[YahooSearch] Resolved to {symbol} (name={name})");
            return new ResolvedSymbol(symbol, name);
        }

        Console.Error.WriteLine($"[YahooSearch] No results for {identifier}");
        return null;
    }
}