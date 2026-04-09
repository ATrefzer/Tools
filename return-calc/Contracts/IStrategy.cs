namespace ReturnCalc.Contracts;

/// <summary>
/// A strategy is called once per price bar and returns a single trade decision.
/// Only historical data (up to and including today) is visible — no look-ahead bias possible.
/// Portfolio simulation and metrics are handled by the runner.
/// </summary>
public interface IStrategy
{
    string Name { get; }

    /// <summary>
    /// Called once per bar. <paramref name="ctx"/> contains the price history up to today
    /// and the current portfolio state before the trade is executed.
    /// </summary>
    TradeAction Decide(DecisionContext ctx);
}
