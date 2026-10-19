#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using E.Standard.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Extensions;

using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.Models;

/// <summary>
/// Describes how a query result was created (query, effective query geometry, form items, where, limit),
/// so the result can be re-executed with an additional QueryBuilder where clause (result restriction).
/// Sent to the client only encrypted (<see cref="ToToken"/>), never as plain text.
/// </summary>
public class QueryRestrictionDescriptor
{
    public const int MaxGeometryVertices = 10000;

    [JsonProperty("service_id")]
    [System.Text.Json.Serialization.JsonPropertyName("service_id")]
    public string ServiceId { get; set; } = "";

    [JsonProperty("query_id")]
    [System.Text.Json.Serialization.JsonPropertyName("query_id")]
    public string QueryId { get; set; } = "";

    /// <summary>
    /// Effective query geometry (incl. buffer, click tolerance, ...) as WKT in <see cref="Srs"/>
    /// </summary>
    [JsonProperty("geometry", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("geometry")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Geometry { get; set; }

    [JsonProperty("srs")]
    [System.Text.Json.Serialization.JsonPropertyName("srs")]
    public int Srs { get; set; }

    /// <summary>
    /// Query (search) items of the original request. Only query item ids, no special keys
    /// </summary>
    [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Items { get; set; }

    /// <summary>
    /// Where clause of the original request (eg. identify with QueryBuilder)
    /// </summary>
    [JsonProperty("where", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("where")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Where { get; set; }

    [JsonProperty("limit")]
    [System.Text.Json.Serialization.JsonPropertyName("limit")]
    public int Limit { get; set; }

    /// <summary>
    /// Origin of the result (only for display), see <see cref="Sources"/>
    /// </summary>
    [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("source")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Source { get; set; }

    /// <summary>
    /// Display names of the <see cref="Items"/> (only for display)
    /// </summary>
    [JsonProperty("item_labels", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("item_labels")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? ItemLabels { get; set; }

    public static class Sources
    {
        public const string Identify = "identify";
        public const string Buffer = "buffer";
        public const string Query = "query";
        public const string MapExtent = "bbox";
        public const string Table = "table";
    }

    /// <summary>
    /// The current restriction (QueryBuilder where clause), null => not restricted
    /// </summary>
    [JsonProperty("restriction", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("restriction")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Restriction { get; set; }

    #region Create

    /// <summary>
    /// Creates a descriptor from the filter, a query was performed with.
    /// Empty ids are completed by the API (see Api TrySetQueryRestriction).
    /// Returns null, if the query geometry is too complex (<see cref="MaxGeometryVertices"/>).
    /// </summary>
    static public QueryRestrictionDescriptor? Create(
                string? serviceId,
                string? queryId,
                ApiQueryFilter? filter,
                IEnumerable<string>? allowedItemKeys = null,
                string? where = null,
                int limit = 0,
                string? source = null)
    {
        var descriptor = new QueryRestrictionDescriptor()
        {
            ServiceId = serviceId ?? "",
            QueryId = queryId ?? "",
            Where = String.IsNullOrWhiteSpace(where) ? null : where,
            Limit = limit,
            Source = source
        };

        if (filter is ApiSpatialFilter spatialFilter && spatialFilter.QueryShape != null)
        {
            if (!descriptor.TrySetGeometry(spatialFilter.QueryShape, spatialFilter.FilterSpatialReference?.Id ?? 0))
            {
                return null;
            }
        }

        if (filter?.QueryItems != null && allowedItemKeys != null)
        {
            descriptor.SetItems(filter.QueryItems, allowedItemKeys);
        }

        return descriptor;
    }

    public bool TrySetGeometry(Shape? shape, int srs)
    {
        if (shape == null)
        {
            Geometry = null;
            Srs = 0;
            return true;
        }

        if (shape.CountPoints() > MaxGeometryVertices || srs <= 0)
        {
            return false;
        }

        Geometry = shape.WKTFromShape();
        Srs = srs;

        return !String.IsNullOrEmpty(Geometry);
    }

    public void SetItems(NameValueCollection? queryItems, IEnumerable<string> allowedItemKeys, Func<string, string?>? itemLabel = null)
    {
        var items = new Dictionary<string, string>();
        var labels = new Dictionary<string, string>();

        foreach (var key in allowedItemKeys.Where(k => IsValidItemKey(k)))
        {
            var value = queryItems?[key];
            if (!String.IsNullOrEmpty(value))
            {
                items[key] = value;

                var label = itemLabel?.Invoke(key);
                if (!String.IsNullOrWhiteSpace(label))
                {
                    labels[key] = label!;
                }
            }
        }

        Items = items.Count > 0 ? items : null;
        ItemLabels = labels.Count > 0 ? labels : null;
    }

    #endregion

    #region Execute

    public Shape? QueryShape() => String.IsNullOrEmpty(Geometry) ? null : Geometry.ShapeFromWKT();

    public NameValueCollection QueryItems()
    {
        var queryItems = new NameValueCollection();

        foreach (var key in Items?.Keys.Where(k => IsValidItemKey(k)) ?? [])
        {
            queryItems[key] = Items![key];
        }

        return queryItems;
    }

    /// <summary>
    /// Original where AND restriction
    /// </summary>
    public string? EffectiveWhereClause(string? restriction)
        => (String.IsNullOrWhiteSpace(Where), String.IsNullOrWhiteSpace(restriction)) switch
        {
            (true, true) => null,
            (false, true) => Where,
            (true, false) => restriction,
            _ => $"({Where}) and ({restriction})"
        };

    #endregion

    #region Token

    public string ToToken(ICryptoService crypto)
        => crypto.EncryptTextDefault(JSerializer.Serialize(this));

    static public QueryRestrictionDescriptor FromToken(ICryptoService crypto, string? token)
    {
        if (String.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Missing query restriction token");
        }

        return JSerializer.Deserialize<QueryRestrictionDescriptor>(crypto.DecryptTextDefault(token))
               ?? throw new ArgumentException("Invalid query restriction token");
    }

    #endregion

    // keys like #oid#, #oids#, ~oid~ are interpreted as raw sql by the query engine => never allowed
    static private bool IsValidItemKey(string? key)
        => !String.IsNullOrEmpty(key)
           && !key.StartsWith("#")
           && !key.StartsWith("~")
           && !key.StartsWith("_");
}
