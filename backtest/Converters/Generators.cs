using System.Text.Json.Serialization;
using StockBacktest.Contracts;
using StockBacktest.Models;

namespace StockBacktest.Converters;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    UseStringEnumConverter = true,
    WriteIndented = true)]
[JsonSerializable(typeof(BacktestResult))]
[JsonSerializable(typeof(YahooSearchResult[]))]
[JsonSerializable(typeof(PricePoint[]))]
[JsonSerializable(typeof(SimulationResult))]
[JsonSerializable(typeof(TradeActionType))]
internal partial class BacktestJsonContext : JsonSerializerContext
{
}