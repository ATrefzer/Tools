namespace StockBacktest.Models;

public class YahooSearchResult
{
    public string Symbol { get; init; } = "";
    public string? Name { get; init; }
    public string? Type { get; init; }
}