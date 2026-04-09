namespace ReturnCalc.Contracts;

/// <summary>
/// A trade decision returned by a strategy.
/// <see cref="Fraction"/> controls how much of the available cash (Buy) or shares (Sell) to trade.
/// </summary>
public sealed record TradeAction(TradeActionType Type, decimal Fraction)
{
    public static readonly TradeAction None = new(TradeActionType.None, 0m);

    /// <summary>Buy <paramref name="fraction"/> of available cash (default: all).</summary>
    public static TradeAction Buy(decimal fraction = 1m) => new(TradeActionType.Buy, fraction);

    /// <summary>Sell <paramref name="fraction"/> of held shares (default: all).</summary>
    public static TradeAction Sell(decimal fraction = 1m) => new(TradeActionType.Sell, fraction);
}
