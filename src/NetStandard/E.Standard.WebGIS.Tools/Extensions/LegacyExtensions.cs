using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core.Api.Extensions;

namespace E.Standard.WebGIS.Tools.Extensions;

static internal class LegacyExtensions
{
    static public string ReplaceLegacyMapJsonItems(this string mapJson)
        => string.IsNullOrEmpty(mapJson)
            ? mapJson
            : mapJson
                .Replace("webgis.tools.redlining.redlining", "webgis.tools.mapmarkup.mapmarkup")
                .Replace("webgis.tools.advanced.redlining", "webgis.tools.advanced.mapmarkup");

    // Maps delivered to the client always carry signed dynamic content urls (see DynamicContentSignature)
    static public string ReplaceLegacyMapJsonItemsAndSignDynamicContent(this string mapJson, ICryptoService crypto)
        => DynamicContentSignature.SignMapJson(mapJson.ReplaceLegacyMapJsonItems(), crypto);
}
