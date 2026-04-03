namespace StockBacktest.Models;

public class YahooQuote
{
    public string? Currency { get; init; }
    public string? LongName { get; init; }
    public string? ShortName { get; init; }
    public float? RegularMarketPrice { get; init; }
    public DateTime? RegularMarketTime { get; init; }
}