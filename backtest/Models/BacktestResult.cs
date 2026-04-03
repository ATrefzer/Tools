namespace StockBacktest.Models;

public record BacktestResult
{
    public string Identifier { get; init; } = "";
    public string IdentifierType { get; init; } = "";
    public string? ResolvedTicker { get; init; }
    public string? ResolvedName { get; init; }
    public string? Currency { get; init; }
    public DateOnly? BuyDate { get; init; }
    public decimal? BuyPrice { get; init; }
    public DateOnly? SellDate { get; init; }
    public decimal? SellPrice { get; init; }
    public bool IsSellDateToday { get; init; }
    public decimal? AbsoluteReturn { get; init; }
    public decimal? PercentReturn { get; init; }
    public decimal? AnnualizedReturn { get; init; }
    public int? HoldingDays { get; init; }
    public string? DataSource { get; init; }
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;
    public List<string> Errors { get; init; } = new();
}