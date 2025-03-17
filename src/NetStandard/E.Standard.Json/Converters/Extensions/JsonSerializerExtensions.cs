using System.Text.Json;
using System.Text.Json.Serialization;

namespace E.Standard.Json.Converters.Extensions;

internal static class JsonSerializerExtensions
{
    public static void WriteOrSerialize<T>(this JsonConverter<T> converter, Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (converter != null)
        {
            converter.Write(writer, value, options);
        }
        else
        {
            JsonSerializer.Serialize(writer, value, typeof(T), options);
        }
    }
}
