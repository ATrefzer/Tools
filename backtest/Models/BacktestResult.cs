using System.Text.Json.Serialization;
using StockBacktest.Converters;

namespace StockBacktest.Models;

public record BacktestResult
{
    public string Ticker { get; init; } = "";
    public string? Name { get; init; }
    public string? Currency { get; init; }

    [JsonConverter(typeof(NullableDateTimeJsonConverter))]
    public DateTime? BuyDate { get; init; }

    public decimal? BuyPrice { get; init; }

    [JsonConverter(typeof(NullableDateTimeJsonConverter))]
    public DateTime? SellDate { get; init; }

    public decimal? SellPrice { get; init; }
    public decimal? AbsoluteReturn { get; set; }
    public decimal? PercentReturn { get; set; }
    public decimal? AnnualizedReturn { get; set; }
    public int? HoldingDays { get; set; }
}