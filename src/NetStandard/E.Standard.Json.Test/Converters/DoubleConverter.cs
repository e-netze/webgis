using System.Text.Json;
using System.Text.Json.Serialization;

namespace E.Standard.Json.Test.Converters;

public class DoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDouble();
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (value == Math.Floor(value))
        {
            writer.WriteRawValue($"{value}.0");
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}
