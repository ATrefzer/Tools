using StockBacktest.Contracts;

namespace StockBacktest.Strategies;

/// <summary>
/// Example strategy X.
///
/// Buy rules — Reference is ath
///   -15% → buy 10% of remaining cash
///   -25% → buy 25% of remaining cash
///
/// Sell rules — Reset whenever we sold
///   +25%  → sell 10%
///   +35%  → sell 20%
///   +45%  → sell 30%
///   +60%  → sell 40%
///   +100% → sell everything
///
/// </summary>
internal class X : IStrategy
{
    public string Name => "X";

    private decimal _buyRef;
    private decimal _sellRef;
    private decimal _priceAtH;

    // Buy the dip
    private static readonly (int Threshold, decimal Fraction)[] BuyRules =
    [
        (-25, 0.25m),
        (-15, 0.10m),
    ];

    // Take profit
    private static readonly (int Threshold, decimal Fraction)[] SellRules =
    [
        (100, 1.00m),
        ( 60, 0.40m),
        ( 45, 0.30m),
        ( 35, 0.20m),
        ( 25, 0.10m),
    ];

    public TradeAction Decide(DecisionContext ctx)
    {
        var price = ctx.History[^1].Price;

        if (ctx.History.Count == 1)
        {
            _buyRef   = price;
            _sellRef  = price;
            _priceAtH = price;
            return TradeAction.Buy((decimal)0.5);
        }

        var oldAtH = _priceAtH;
        
     
        if (price > _priceAtH)
        {
            _priceAtH = price;
            _buyRef   = price;
        }

        // Take profit
        var sellChange = (price - _sellRef) / _sellRef * 100m;
            _priceAtH = price;

            foreach (var (threshold, fraction) in SellRules)
            {
                if (sellChange >= threshold && ctx.Portfolio.Shares > 0)
                {
                    _sellRef = price;  // trail sell reference forward only on an actual sell
                    return TradeAction.Sell(fraction);
                }
            }
        

        // Buy the dip (buyRef == ath)
        var buyChange = (price - _buyRef) / _buyRef * 100m;
        foreach (var (threshold, fraction) in BuyRules)
        {
            if (buyChange <= threshold && ctx.Portfolio.Cash > 0)
            {
                _sellRef = price;
                return TradeAction.Buy(fraction);
            }
        }

        return TradeAction.None;
    }
}
