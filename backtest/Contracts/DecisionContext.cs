
namespace StockBacktest.Contracts;

public class DecisionContext
{
    /// <summary>
    /// All price points from the start-up to and including today.
    /// <c>History[^1]</c> is the current bar. Future data is never visible.
    /// </summary>
    public required IReadOnlyList<PricePoint> History { get; init; }

    /// <summary>Portfolio state before this step's trade is executed.</summary>
    public required PortfolioSnapshot Portfolio { get; init; }
}

