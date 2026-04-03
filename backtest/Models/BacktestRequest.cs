namespace StockBacktest.Models;

public record BacktestRequest
{
    public string Identifier { get; init; } = "";
    public IdentifierType Type { get; init; } = IdentifierType.ISIN;
    public DateOnly BuyDate { get; init; }
    public DateOnly? SellDate { get; init; } // null = heute
}