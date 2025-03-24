namespace E.Standard.WebGIS.Tools.Extensions;
static internal class LegacyExtensions
{
    static public string ReplaceLegacyMapJsonItems(this string mapJson)
        => string.IsNullOrEmpty(mapJson)
            ? mapJson
            : mapJson
                .Replace("webgis.tools.redlining.redlining", "webgis.tools.mapmarkup.mapmarkup")
                .Replace("webgis.tools.advanced.redlining", "webgis.tools.advanced.mapmarkup");
}
