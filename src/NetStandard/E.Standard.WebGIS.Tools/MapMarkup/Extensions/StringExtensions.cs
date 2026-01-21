using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.MapMarkup.Extensions;

internal static class StringExtensions
{
    static private Dictionary<string, string> LegacySymbolMap = new()
    {
        { "graphics/markers/hotspot0.gif", "graphics/markers/hotspot03.gif"},
        { "graphics/markers/hotspot1.gif", "graphics/markers/hotspot04.gif"},
        { "graphics/markers/hotspot2.gif", "graphics/markers/hotspot05.gif"},
        { "graphics/markers/hotspot3.gif", "graphics/markers/hotspot06.png"},
        { "graphics/markers/hotspot4.gif", "graphics/markers/hotspot07.png"},
        { "graphics/markers/hotspot5.gif", "graphics/markers/hotspot08.png"},
        { "graphics/markers/hotspot6.gif", "graphics/markers/hotspot02.gif"},
        { "graphics/markers/marker_red.gif", "graphics/markers/hotspot01.gif"},
        { "graphics/markers/pin1.png", "graphics/markers/hotspot00.png"}
    };

    public static string ReplaceLegacySymbols(this string input)
    {
        foreach (var kvp in LegacySymbolMap)
        {
            input = input.Replace(kvp.Key, kvp.Value);
        }
        return input;
    }
}
