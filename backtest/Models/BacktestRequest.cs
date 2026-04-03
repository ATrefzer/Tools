namespace StockBacktest.Models;

public record BacktestRequest
{
    public string Ticker { get; init; } = "";
    public DateTime? BuyDate { get; init; } // null = latest known price
    public DateTime SellDate { get; init; }
}