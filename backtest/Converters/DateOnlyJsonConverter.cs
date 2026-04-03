using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockBacktest;

internal sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
    {
        return DateOnly.ParseExact(reader.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions o)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}