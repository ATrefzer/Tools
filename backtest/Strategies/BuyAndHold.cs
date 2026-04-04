using StockBacktest.Contracts;

namespace StockBacktest.Strategies;

/// <summary>
///     Buys everything on the first bar and holds until the end.
///     Used as the reference benchmark — every other strategy must beat this.
/// </summary>
internal class BuyAndHold : IStrategy
{
    public string Name => "BuyAndHold";

    public TradeAction Decide(DecisionContext ctx)
    {
        return ctx.History.Count == 1 ? TradeAction.Buy() : TradeAction.None;
    }
}