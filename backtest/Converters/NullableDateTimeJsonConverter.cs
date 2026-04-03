using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockBacktest.Converters;

internal sealed class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
    {
        var s = reader.GetString();
        return s is null ? null : DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions o)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
    }
}