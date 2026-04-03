using StockBacktest.Models;

namespace StockBacktest.Services;

public record ResolvedSymbol(string Ticker, string? Name);

public interface ISymbolResolver
{
    Task<ResolvedSymbol?> ResolveAsync(string identifier, IdentifierType type, CancellationToken ct = default);
}