namespace StockBacktest.Services;

public interface IPriceProvider
{
    string Name { get; }

    /// <summary>
    ///     Returns the closing price for the given ticker on or after the given date
    ///     (skips non-trading days). Returns null if no data found.
    /// </summary>
    Task<(decimal Price, DateOnly ActualDate)?> GetPriceAsync(string ticker, DateOnly date,
        CancellationToken ct = default);
}