using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

/// <summary>
/// Response of a "returnIdsOnly=true" query against a MapServer/FeatureServer layer.
/// Unlike a normal feature query, this is not affected by the ArcGIS Server bounding-box/TOP
/// bug for spatial queries (see AgsQuerySettings), so it can be used to reliably determine
/// the full set of matching object ids before fetching the actual features in batches.
/// </summary>
class JsonFeatureIdsResponse
{
    [JsonProperty("objectIdFieldName")]
    [System.Text.Json.Serialization.JsonPropertyName("objectIdFieldName")]
    public string ObjectIdFieldName { get; set; }

    [JsonProperty("objectIds")]
    [System.Text.Json.Serialization.JsonPropertyName("objectIds")]
    public long[] ObjectIds { get; set; }
}
