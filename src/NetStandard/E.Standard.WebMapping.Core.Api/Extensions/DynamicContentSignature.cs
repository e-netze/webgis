using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

using E.Standard.Security.Cryptography.Abstractions;

namespace E.Standard.WebMapping.Core.Api.Extensions;

/// <summary>
/// Signs the urls of dynamic content (GeoRSS/GeoJSON) that are downloaded server-side by
/// <c>DynamicContentProxy</c>. The signature is appended to the url (<c>url#webgis-sig=...</c>) and
/// binds the content type and the url. Maps get signed urls when they are loaded; saving/sharing
/// a map with unsigned dynamic content is rejected, so clients cannot introduce arbitrary urls.
/// </summary>
static public class DynamicContentSignature
{
    private const string Marker = "#webgis-sig=";

    static public bool RequiresSignature(string type)
        => !String.IsNullOrEmpty(type) && type != TocApiQueryType;

    /// <summary>
    /// Dynamic content of a TOC theme: the url is only <c>serviceId@queryId</c>; the server builds the query
    /// itself and checks existence/permissions, so there is nothing to sign.
    /// </summary>
    public const string TocApiQueryType = "toc-api-query";

    static public string Sign(ICryptoService crypto, string type, string url)
    {
        Unwrap(crypto, type, url, out bool signed);
        if (signed || String.IsNullOrEmpty(url))
        {
            return url;
        }

        string plainUrl = StripMarker(url);
        return $"{plainUrl}{Marker}{CalcSignature(crypto, type, plainUrl)}";
    }

    /// <summary>Removes the signature from the url and tells whether it was valid.</summary>
    static public string Unwrap(ICryptoService crypto, string type, string url, out bool signed)
    {
        signed = false;

        int index = url?.LastIndexOf(Marker, StringComparison.Ordinal) ?? -1;
        if (index < 0)
        {
            return url;
        }

        string plainUrl = url.Substring(0, index);
        string signature = url.Substring(index + Marker.Length);

        signed = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(CalcSignature(crypto, type, plainUrl)));

        return plainUrl;
    }

    /// <summary>Signs all unsigned dynamic content urls of a map json.</summary>
    static public string SignMapJson(string mapJson, ICryptoService crypto)
        => ProcessMapJson(mapJson, crypto, requireSigned: false);

    /// <summary>Throws if a map json contains dynamic content urls without a valid signature.</summary>
    static public void EnsureMapJsonSigned(string mapJson, ICryptoService crypto)
        => ProcessMapJson(mapJson, crypto, requireSigned: true);

    private static string ProcessMapJson(string mapJson, ICryptoService crypto, bool requireSigned)
    {
        if (String.IsNullOrWhiteSpace(mapJson)
            || mapJson.IndexOf("dynamiccontent", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return mapJson;
        }

        JsonNode root;
        try
        {
            root = JsonNode.Parse(mapJson);
        }
        catch (System.Text.Json.JsonException)
        {
            return mapJson;
        }

        if (root is not JsonObject rootObject || rootObject["dynamiccontent"] is not JsonArray items)
        {
            return mapJson;
        }

        bool changed = ProcessItems(items, crypto, requireSigned);

        return changed ? rootObject.ToJsonString() : mapJson;
    }

    /// <summary>Signs all unsigned urls of a json array of dynamic content (e.g. for MapBuilder html snippets).</summary>
    static public string SignContentJson(string contentArrayJson, ICryptoService crypto)
    {
        if (String.IsNullOrWhiteSpace(contentArrayJson))
        {
            return contentArrayJson;
        }

        if (JsonNode.Parse(contentArrayJson) is not JsonArray items)
        {
            return contentArrayJson;
        }

        return ProcessItems(items, crypto, requireSigned: false) ? items.ToJsonString() : contentArrayJson;
    }

    private static bool ProcessItems(JsonArray items, ICryptoService crypto, bool requireSigned)
    {
        bool changed = false;
        foreach (var item in items)
        {
            if (item is not JsonObject content
                || content["url"] is not JsonValue urlValue
                || !urlValue.TryGetValue(out string url)
                || !content.TryGetPropertyValue("type", out var typeNode)
                || !RequiresSignature(typeNode?.GetValue<string>()))
            {
                continue;
            }

            string type = typeNode.GetValue<string>();
            Unwrap(crypto, type, url, out bool signed);

            if (signed)
            {
                continue;
            }

            if (requireSigned)
            {
                throw new InvalidOperationException("Map contains dynamic content without a valid signature. Reload the map and save it again.");
            }

            content["url"] = Sign(crypto, type, url);
            changed = true;
        }

        return changed;
    }

    private static string StripMarker(string url)
    {
        int index = url.LastIndexOf(Marker, StringComparison.Ordinal);
        return index < 0 ? url : url.Substring(0, index);
    }

    private static string CalcSignature(ICryptoService crypto, string type, string url)
        => Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(
                crypto.StaticDefaultEncrypt_Aes($"dynamic-content|{type}|{url}"))));
}
