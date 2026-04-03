using System.Text;
using System.Text.Json;
using StockBacktest.Converters;
using StockBacktest.Models;

namespace StockBacktest.Services;

public sealed class OpenFigiResolver : ISymbolResolver
{
    // Exchange code → Yahoo Finance suffix
    private static readonly Dictionary<string, string?> ExchangeSuffix = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GY"] = ".DE", // XETRA
        ["GF"] = ".F", // Frankfurt
        ["US"] = "", // NYSE / NASDAQ
        ["UN"] = "", // NYSE
        ["UQ"] = "", // NASDAQ
        ["LN"] = ".L", // London
        ["FP"] = ".PA", // Paris
        ["SM"] = ".MC", // Madrid
        ["IM"] = ".MI", // Milan
        ["NA"] = ".AS", // Amsterdam
        ["BB"] = ".BR", // Brussels
        ["SW"] = ".SW", // Swiss
        ["AU"] = ".AX", // Australia
        ["JP"] = ".T" // Tokyo
    };

    private readonly HttpClient _http;

    public OpenFigiResolver(HttpClient http)
    {
        _http = http;
    }

    public async Task<ResolvedSymbol?> ResolveAsync(
        string identifier, IdentifierType type, CancellationToken ct = default)
    {
        var idType = type switch
        {
            IdentifierType.ISIN => "ID_ISIN",
            IdentifierType.WKN => "ID_WERTPAPIER",
            _ => null
        };

        if (idType is null)
        {
            return null; // Ticker needs no resolution
        }

        OpenFigiRequest[] requestDto = [new OpenFigiRequest(idType, identifier)];
        var body = JsonSerializer.Serialize(requestDto, BacktestJsonContext.Default.OpenFigiRequestArray);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        Console.Error.WriteLine($"[OpenFIGI] Resolving {type} {identifier}");

        using var response = await _http.PostAsync("https://api.openfigi.com/v3/mapping", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"[OpenFIGI] HTTP {(int)response.StatusCode}");
            return null;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            return null;
        }

        var first = root[0];
        if (!first.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array ||
            data.GetArrayLength() == 0)
        {
            return null;
        }

        // Collect all candidates with known exchange codes
        var candidates = new List<(string Ticker, string ExchCode, string? Suffix, string? Name)>();

        foreach (var item in data.EnumerateArray())
        {
            if (!item.TryGetProperty("ticker", out var tickerEl))
            {
                continue;
            }

            if (!item.TryGetProperty("exchCode", out var exchEl))
            {
                continue;
            }

            var ticker = tickerEl.GetString();
            var exchCode = exchEl.GetString();

            if (string.IsNullOrEmpty(ticker) || string.IsNullOrEmpty(exchCode))
            {
                continue;
            }

            if (!ExchangeSuffix.TryGetValue(exchCode, out var suffix))
            {
                continue;
            }

            var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            candidates.Add((ticker, exchCode, suffix, name));
        }

        if (candidates.Count > 0)
        {
            // Prefer exchange based on ISIN country prefix
            var isinCountry = identifier.Length >= 2 ? identifier[..2].ToUpperInvariant() : "";
            var preferred = isinCountry switch
            {
                "DE" => new[] { "GY", "GF" }, // Germany → XETRA, Frankfurt
                "US" => new[] { "US", "UN", "UQ" }, // USA
                "GB" => new[] { "LN" }, // UK
                "FR" => new[] { "FP" }, // France
                "ES" => new[] { "SM" }, // Spain
                "IT" => new[] { "IM" }, // Italy
                "NL" => new[] { "NA" }, // Netherlands
                "BE" => new[] { "BB" }, // Belgium
                "CH" => new[] { "SW" }, // Switzerland
                "AU" => new[] { "AU" }, // Australia
                "JP" => new[] { "JP" }, // Japan
                _ => Array.Empty<string>()
            };

            // Try preferred exchanges first
            foreach (var pref in preferred)
            {
                var match = candidates.FirstOrDefault(c => c.ExchCode.Equals(pref, StringComparison.OrdinalIgnoreCase));
                if (match != default)
                {
                    var yahooTicker = match.Ticker + match.Suffix;
                    Console.Error.WriteLine(
                        $"[OpenFIGI] Resolved to {yahooTicker} (exchCode={match.ExchCode}, name={match.Name}) [preferred]");
                    return new ResolvedSymbol(yahooTicker, match.Name);
                }
            }

            // Fall back to first known candidate
            var first2 = candidates[0];
            var fallbackTicker = first2.Ticker + first2.Suffix;
            Console.Error.WriteLine(
                $"[OpenFIGI] Resolved to {fallbackTicker} (exchCode={first2.ExchCode}, name={first2.Name})");
            return new ResolvedSymbol(fallbackTicker, first2.Name);
        }

        return null;
    }
}