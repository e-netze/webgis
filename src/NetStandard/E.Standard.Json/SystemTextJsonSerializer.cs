using E.Standard.Json.Abstractions;
using E.Standard.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace E.Standard.Json;
internal class SystemTextJsonSerializer : IJSerializer
{
    internal static JsonSerializerOptions DefaultSerializerOptions =
        new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            Converters =
            {
                new Array2DConverter(),
                new DBNullConverter(),
                new ExpandoObjectConverter(),
                //new JsonStringEnumConverter(),
            }
        };
    internal static JsonSerializerOptions DefaultSerializerOptionsPretty =
        new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

            Converters =
            {
                new Array2DConverter(),
                new DBNullConverter(),
                new ExpandoObjectConverter(),
                //new JsonStringEnumConverter(),
            },
            WriteIndented = true,
        };

    internal static JsonSerializerOptions DefaultDeserializerOptions = new JsonSerializerOptions()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters =
        {
            new Array2DConverter(),
            new DBNullConverter(),
            new ExpandoObjectConverter(),
            new DictionaryStringObjectJsonConverter(),
            new JsonStringEnumConverter()
        }
    };

    public T? Deserialize<T>([StringSyntax("Json")] string json)
        => String.IsNullOrEmpty(json)
        ? default(T?)
        : JsonSerializer.Deserialize<T>(json, DefaultDeserializerOptions);

    public object? Deserialize([StringSyntax("Json")] string json, Type type)
        => String.IsNullOrEmpty(json)
        ? null
        : JsonSerializer.Deserialize(json, type, DefaultDeserializerOptions);

    public string Serialize<TValue>(TValue value, bool pretty = false)
        => pretty
            ? JsonSerializer.Serialize(value, DefaultSerializerOptionsPretty)
            : JsonSerializer.Serialize(value, DefaultSerializerOptions);

    public bool IsJsonElement(object element) => element is JsonElement;
    public object? GetJsonElementValue<T>(object element)
        => element is JsonElement jsonElement
                ? true switch
                {
                    _ when typeof(T) == typeof(System.Int32) => jsonElement.GetInt32(),
                    _ when typeof(T) == typeof(System.Int64) => jsonElement.GetInt64(),
                    _ when typeof(T) == typeof(System.Single) => jsonElement.GetSingle(),
                    _ when typeof(T) == typeof(System.Double) => jsonElement.GetDouble(),
                    _ when typeof(T) == typeof(System.String) => jsonElement.GetString(),
                    _ => throw new ArgumentException($"Unsuppoerted JSON Element Type in GetJsonElementValue: {typeof(T)}")
                }
                : null;
    public object? GetJsonElementValue(object element, string propertyName)
    {
        var propertyNameParts = propertyName.Split('.').ToArray();
        object? result = null;

        for (int i = 0; i < propertyNameParts.Length; i++)
        {
            if (element is JsonElement jsonElement && jsonElement.TryGetProperty(propertyNameParts[i], out JsonElement propertyElement))
            {
                result = propertyElement.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => propertyElement.GetString(),
                    JsonValueKind.Number => propertyElement.GetDouble(),
                    _ => i == propertyNameParts.Length - 1
                        ? propertyElement // throw new ArgumentException($"Unsuppoerted JSON Element ValueKind in GetJsonElementValue: {propertyElement.ValueKind}")
                        : propertyElement
                };

                if (i < propertyNameParts.Length - 1)
                {
                    element = result!;
                }
            }
            else if (element is IDictionary<string, object> dict && dict.ContainsKey(propertyNameParts[i]))
            {
                result = ((IDictionary<string, object>)element)[propertyNameParts[i]];

                if (i < propertyNameParts.Length - 1)
                {
                    element = result!;
                }
            }
            else if (element is not JsonElement && element?.GetType().GetProperty(propertyNameParts[i]) != null)
            {
                result = element?.GetType().GetProperty(propertyNameParts[i]);

                if (i < propertyNameParts.Length - 1)
                {
                    element = result!;
                }
            }
        }

        return result;
    }

    public IEnumerable<string> GetJsonElementProperties(object? element)
    {
        if (element is JsonElement jsonElement
            && jsonElement.ValueKind == JsonValueKind.Object)
        {
            return jsonElement.EnumerateObject().Select(p => p.Name);
        }
        else if (element is IDictionary<string, object> dict)
        {
            return dict.Keys;
        }

        return [];
    }

    public object? AsValueIfJsonValueType(object? element)
        => element is JsonElement jsonElement
            ? jsonElement.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.GetDouble(),
                _ => element
            }
            : element;
}

public class JsonOptions
{
    static public JsonSerializerOptions[] SerializerOptions = [SystemTextJsonSerializer.DefaultSerializerOptions, SystemTextJsonSerializer.DefaultSerializerOptionsPretty];
    static public JsonSerializerOptions[] DeserializerOptions = [SystemTextJsonSerializer.DefaultDeserializerOptions];
}
