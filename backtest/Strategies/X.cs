using StockBacktest.Contracts;

namespace StockBacktest.Strategies;

/// <summary>
///     Bad example strategy.
/// 
///     Buy rules — Reference is ath
///     -15% → buy 10% of remaining cash
///     -25% → buy 25% of remaining cash
/// 
///     Sell rules — reference is low from ath
///     +25%  → sell 10%
///     +35%  → sell 20%
///     +45%  → sell 30%
///     +60%  → sell 40%
///     +100% → sell everything
/// </summary>
internal class X : IStrategy
{
    // Buy the dip
    private static readonly (int Threshold, decimal Fraction)[] BuyRules =
    [
        (-25, 0.25m),
        (-15, 0.10m)
    ];

    // Take profit
    private static readonly (int Threshold, decimal Fraction)[] SellRules =
    [
        (100, 1.00m),
        (60, 0.40m),
        (45, 0.30m),
        (35, 0.20m),
        (25, 0.10m)
    ];

    private decimal _buyRef;
    private decimal _sellRef;
    private decimal _lowSinceAtH;
    private decimal _priceAtH;
    public string Name => "X";

    public TradeAction Decide(DecisionContext ctx)
    {
        var price = ctx.History[^1].Price;

        if (ctx.History.Count == 1)
        {
            _priceAtH = price;
            _lowSinceAtH = price;
            _sellRef = price;
            _buyRef = price;
            return TradeAction.Buy((decimal)1);
        }


        if (price > _priceAtH)
        {
            _priceAtH = price;
            _sellRef = price;
            _lowSinceAtH = price;

            // Take profit
            var sellChange = (price - _sellRef) / _sellRef * 100m;

            foreach (var (threshold, fraction) in SellRules)
            {
                if (sellChange >= threshold && ctx.Portfolio.Shares > 0)
                {
                    return TradeAction.Sell(fraction);
                }
            }
        }
        else if (price < _lowSinceAtH)
        {
            _buyRef = price;
            _lowSinceAtH = price;

            // Buy the dip 
            var buyChange = (price - _buyRef) / _buyRef * 100m;
            foreach (var (threshold, fraction) in BuyRules)
            {
                if (buyChange <= threshold && ctx.Portfolio.Cash > 0)
                {
                    return TradeAction.Buy(fraction);
                }
            }
        }


        return TradeAction.None;
    }
}