using E.Standard.Json;
using System;

namespace E.Standard.WebGIS.Tools.Identify.Extensions;

static internal class StringExtensions
{
    public static T TryParseJson<T>(this string json, T defaultValue = default(T))
    {
        if (String.IsNullOrWhiteSpace(json))
        {
            return defaultValue;
        }

        try
        {
            return JSerializer.Deserialize<T>(json);
        }
        catch { }
        return defaultValue;
    }
}
