using System.Collections.Generic;
using System.Text.Json;

namespace E.Standard.Json.Extensions;
static internal class JsonReaderExtensions
{
    static public object? ExtractValue(this ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString()!;
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int i))
                {
                    return i;
                }
                if (reader.TryGetInt64(out long l))
                {
                    return l;
                }
                return reader.GetDouble();
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.StartArray:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(ExtractValue(ref reader, options));
                }
                return list;
            default:
                // for complexe or unknown types return JsonElement
                using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.Clone();
                }
        }
    }
}
