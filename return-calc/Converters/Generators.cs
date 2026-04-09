using System.Text.Json.Serialization;
using ReturnCalc.Contracts;
using ReturnCalc.Models;

namespace ReturnCalc.Converters;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    UseStringEnumConverter = true,
    WriteIndented = true)]
[JsonSerializable(typeof(ResultDto))]
[JsonSerializable(typeof(YahooSearchResult[]))]
[JsonSerializable(typeof(PricePoint[]))]
[JsonSerializable(typeof(SimulationResult))]
[JsonSerializable(typeof(TradeActionType))]
internal partial class BacktestJsonContext : JsonSerializerContext
{
}