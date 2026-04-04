using StockBacktest.Contracts;

namespace StockBacktest.Models;

public class SimulationStep
{
    public DateTime Date { get; init; }
    public decimal Price { get; init; }
    public TradeActionType Action { get; init; }
    public decimal Fraction { get; init; }
    public decimal Shares { get; init; }
    public decimal Cash { get; init; }
    public decimal PortfolioValue { get; init; }

    /// <summary>Portfolio value high-water mark up to and including this step.</summary>
    public decimal AllTimeHigh { get; init; }

    /// <summary>Drawdown from the high-water mark in percent (0 at ATH, negative below).</summary>
    public decimal PercentFromAllTimeHigh { get; init; }
}