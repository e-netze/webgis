using E.Standard.Json.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace E.Standard.Json.Converters;

internal class DictionaryStringObjectJsonConverter : JsonConverter<IDictionary<string, object>>
{
    public override IDictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected JSON-Object not found {reader.TokenType}.");
        }

        var dictionary = new Dictionary<string, object>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected JSON-PropertyName not found {reader.TokenType}.");
            }

            string propertyName = reader.GetString()!;
            reader.Read();
            dictionary[propertyName] = reader.ExtractValue(options)!;
        }

        throw new JsonException("JSON-Object not closed correctly.");
    }

    public override void Write(Utf8JsonWriter writer, IDictionary<string, object> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
