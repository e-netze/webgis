using E.Standard.Platform;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace E.Standard.Json.Converters;

public class StringArrayConverter : JsonConverter<string?[]>
{
    public override string?[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected StartArray token");

        var list = new List<string?>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    list.Add(reader.GetString());
                    break;
                case JsonTokenType.Number:
                    list.Add(reader.GetDouble().ToPlatformNumberString());
                    break;
                case JsonTokenType.True:
                    list.Add("true");
                    break;
                case JsonTokenType.False:
                    list.Add("false");
                    break;
                case JsonTokenType.Null:
                    list.Add(null);
                    break;
                case JsonTokenType.StartObject:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        list.Add(doc.RootElement.GetRawText());
                    }
                    break;
                case JsonTokenType.StartArray:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        list.Add(doc.RootElement.GetRawText());
                    }
                    break;
                default:
                    throw new JsonException($"Unsupported token type in array: {reader.TokenType}");
            }
        }

        return list.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, string?[] values, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        if (values != null)
        {
            foreach (var value in values)
            {
                writer.WriteStringValue(value);
            }
        }
        
        writer.WriteEndArray();
    }
}