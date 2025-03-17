using E.Standard.Json.Abstractions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace E.Standard.Json;
internal class NewtonSoftSerializer : IJSerializer
{
    public T? Deserialize<T>([StringSyntax("Json")] string json)
        => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);

    public object? Deserialize([StringSyntax("Json")] string json, Type type)
        => Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);

    public string Serialize<TValue>(TValue value, bool pretty = false)
    {
        using (MemoryStream ms = new MemoryStream())
        using (StreamWriter sw = new StreamWriter(ms))
        {
            var jw = new Newtonsoft.Json.JsonTextWriter(sw);
            jw.Formatting = pretty
                ? Newtonsoft.Json.Formatting.Indented
                : Newtonsoft.Json.Formatting.None;
            var serializer = new Newtonsoft.Json.JsonSerializer();
            serializer.Serialize(jw, value);
            jw.Flush();
            ms.Position = 0;

            string jsonString = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            //jsonString = jsonString.Trim('\0');

            return jsonString;
        }
    }

    public bool IsJsonElement(object element) => element is JObject;
    public object? GetJsonElementValue<T>(object element)
        => element is JObject jObject
                ? jObject.Value<T>()
                : null;

    public object? GetJsonElementValue(object element, string propertyName)
    {
        var propertyNameParts = propertyName.Split('.').ToArray();
        object? result = null;

        for (int i = 0; i < propertyNameParts.Length; i++)
        {
            if (element is JObject jObject)
            {
                result = jObject.GetValue(propertyNameParts[i]);

                if (result is JValue jVal)
                {
                    result = jVal.Value;
                }

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
            else if (element is not JObject && element?.GetType().GetProperty(propertyNameParts[i]) != null)
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

    public object? AsValueIfJsonValueType(object element)
        => element is JValue jVal
            ? jVal.Value
            : element;
}
