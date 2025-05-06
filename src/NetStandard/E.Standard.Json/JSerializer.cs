using E.Standard.Json.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace E.Standard.Json;

public enum JsonEngine
{
    SytemTextJson,
    NewtonSoft
}

public class JSerializer
{
    static JSerializer() => SetEngine(JsonEngine.SytemTextJson);

    public static void SetEngine(JsonEngine engine)
    {
        if (engine == JsonEngine.NewtonSoft)
        {
            SerializerInstance = new NewtonSoftSerializer();
        }
        else
        {
            SerializerInstance = new SystemTextJsonSerializer();
        }
    }

    internal static IJSerializer SerializerInstance = null!;
    // new SystemTextJsonSerializer();
    // new NewtonSoftSerializer();

    public static T? Deserialize<T>([StringSyntax("Json")] string json)
        => SerializerInstance.Deserialize<T>(json);

    public static object? Deserialize([StringSyntax("Json")] string json, Type type)
        => SerializerInstance.Deserialize(json, type);

    public static string Serialize<TValue>(TValue value, bool pretty = false)
        => SerializerInstance.Serialize(value, pretty);

    public static bool IsJsonElement(object? element)
        => element is null
            ? false
            : SerializerInstance.IsJsonElement(element);

    public static object? GetJsonElementValue(object element, string propertyName)
        => SerializerInstance.GetJsonElementValue(element, propertyName);

    public static object? AsValueIfJsonValueType(object? element)
        => IsJsonElement(element)
                ? SerializerInstance.AsValueIfJsonValueType(element)
                : element;
}
