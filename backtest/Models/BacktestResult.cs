using System.Text.Json.Serialization;
using StockBacktest.Converters;

namespace StockBacktest.Models;

public class BacktestResult
{
    public string Ticker { get; set; } = "";
    public string? Name { get; init; }
    public string? Currency { get; set; }

    [JsonConverter(typeof(NullableDateTimeJsonConverter))]
    public DateTime? BuyDate { get; set; }

    public decimal? BuyPrice { get; set; }

    [JsonConverter(typeof(NullableDateTimeJsonConverter))]
    public DateTime? SellDate { get; set; }

    public decimal? SellPrice { get; set; }
    public decimal? AbsoluteReturn { get; set; }
    public decimal? PercentReturn { get; set; }
    public decimal? AnnualizedReturn { get; set; }
    public int? HoldingDays { get; set; }

    public bool HasBuyAndSell()
    {
        return BuyDate != null && BuyPrice != null && SellDate != null && SellPrice != null;
    }
}