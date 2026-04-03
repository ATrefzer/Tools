using System.Text.Json.Serialization;
using StockBacktest.Models;

namespace StockBacktest.Converters;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = true)]
[JsonSerializable(typeof(BacktestResult))]
internal partial class BacktestJsonContext : JsonSerializerContext
{
}