using System.Text.Json;
using System.Text.Json.Serialization;

namespace E.Standard.Json.Test.Converters;

public class FloatConverter : JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetSingle();
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
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
