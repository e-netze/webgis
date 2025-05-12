using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace E.Standard.Json.Converters;

[Obsolete("solve this with [System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))] on the specific attribute")]
public class NumberToStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                // Handle both Int32 and Double types
                return reader
                    .GetDouble()
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);

            case JsonTokenType.String:
                return reader.GetString();

            default:
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing a string.");
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
