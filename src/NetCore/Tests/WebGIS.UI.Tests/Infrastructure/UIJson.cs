using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using E.Standard.Json;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace WebGIS.UI.Tests.Infrastructure;

/// <summary>
/// Serializes UI elements exactly like the WebGIS API does:
/// JSerializer (System.Text.Json) with the polymorphic IUIElement/IUISetter converters,
/// that the API registers at startup (Api.Core.AppCode.Extensions.JsonSerializerOptionsExtensions.AddServerDefaults).
/// </summary>
internal static class UIJson
{
    private static readonly object _lock = new();
    private static bool _initialized;

    public static string Serialize(params IUIElement[] elements) => Serialize((IEnumerable<IUIElement>)elements);

    public static string Serialize(IEnumerable<IUIElement> elements)
    {
        EnsureServerDefaults();
        return JSerializer.Serialize(elements.ToArray());
    }

    /// <summary>
    /// Serializes a single element and merges additional JSON properties into it.
    /// For properties the JS builder understands but the C# class doesn't provide (e.g. onchange, iscollapsable).
    /// </summary>
    public static string SerializeWith(IUIElement element, object extraProperties)
    {
        var array = JsonNode.Parse(Serialize(element))!.AsArray();
        var target = array[0]!.AsObject();
        var extra = JsonSerializer.SerializeToNode(extraProperties)!.AsObject();

        foreach (var (key, value) in extra.ToList())
        {
            extra.Remove(key);
            target[key] = value;
        }

        return array.ToJsonString();
    }

    public static string SerializeSetters(IEnumerable<IUISetter> setters)
    {
        EnsureServerDefaults();
        return JSerializer.Serialize(setters.ToArray());
    }

    private static void EnsureServerDefaults()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            foreach (var options in JsonOptions.SerializerOptions)
            {
                options.Converters.Add(new RuntimeTypeConverter<IUIElement>());
                options.Converters.Add(new RuntimeTypeConverter<IUISetter>());
            }
            _initialized = true;
        }
    }

    /// <summary>
    /// Same behaviour as Api.Core.AppCode.Json.Converters.GenericPolymorphicConverter&lt;T&gt;
    /// (copied to avoid referencing the Api web project): serialize with the runtime type.
    /// </summary>
    private sealed class RuntimeTypeConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                JsonSerializer.Serialize(writer, (object)null!, options);
                return;
            }

            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
