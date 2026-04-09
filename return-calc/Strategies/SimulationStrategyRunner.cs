using ReturnCalc.Contracts;
using ReturnCalc.Models;

namespace ReturnCalc.Strategies;

internal static class SimulationStrategyRunner
{
    public static SimulationResult Run(
        IStrategy strategy,
        IReadOnlyList<PricePoint> prices,
        decimal initialCapital = 1000m)
    {
        // ArraySegment<T> implements IReadOnlyList<T> — lets us slice without allocation
        var pricesArray = prices as PricePoint[] ?? prices.ToArray();

        var cash = initialCapital;
        var shares = 0m;
        var allTimeHigh = initialCapital;

        var steps = new List<SimulationStep>(pricesArray.Length);

        for (var i = 0; i < pricesArray.Length; i++)
        {
            var price = pricesArray[i].Price;

            var portfolio = new PortfolioSnapshot
            {
                Cash = cash,
                Shares = shares,
                CurrentPrice = price
            };

            var ctx = new DecisionContext
            {
                // History grows by one each step — strategy never sees future prices
                History = new ArraySegment<PricePoint>(pricesArray, 0, i + 1),
                Portfolio = portfolio
            };

            var action = strategy.Decide(ctx);

            // Execute trade at today's price
            if (action.Type == TradeActionType.Buy && cash > 0)
            {
                var spend = cash * action.Fraction;

                var sharesToBuy = Math.Round(spend / price, 0);

                shares += sharesToBuy;
                cash -= sharesToBuy * price;
            }
            else if (action.Type == TradeActionType.Sell && shares > 0)
            {
                var sharesToSell = Math.Round(shares * action.Fraction, 0);
                cash += sharesToSell * price;
                shares -= sharesToSell;
            }

            var portfolioValue = cash + shares * price;
            allTimeHigh = Math.Max(allTimeHigh, portfolioValue);
            var percentFromATH = Math.Round((portfolioValue - allTimeHigh) / allTimeHigh * 100m, 2);

            steps.Add(new SimulationStep
            {
                Date = pricesArray[i].Date,
                Price = price,
                Action = action.Type,
                Fraction = action.Fraction,
                Shares = Math.Round(shares, 6),
                Cash = Math.Round(cash, 2),
                PortfolioValue = Math.Round(portfolioValue, 2),
                AllTimeHigh = Math.Round(allTimeHigh, 2),
                PercentFromAllTimeHigh = percentFromATH
            });
        }

        var finalValue = steps.Count > 0 ? steps[^1].PortfolioValue : initialCapital;
        var returnPercent = Math.Round((finalValue - initialCapital) / initialCapital * 100m, 2);

        return new SimulationResult
        {
            Strategy = strategy.Name,
            InitialCapital = initialCapital,
            FinalCapital = finalValue,
            ReturnPercent = returnPercent,
            Steps = steps
        };
    }
}