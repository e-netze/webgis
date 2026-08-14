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

    /// <summary>
    /// Set to <c>true</c> if the requested <c>resultRecordCount</c> caused the server to clamp
    /// the returned ids to the service's <c>maxRecordCount</c>. Only reliably reported by
    /// ArcGIS Server >= 11.5; AGS 11.2 clamps the same way but does NOT set this flag (see
    /// <see cref="AgsQuerySettings"/>/<c>QueryService</c> for details) - do not rely on this
    /// being <c>false</c> as proof that no clamping occurred on older AGS versions.
    /// </summary>
    [JsonProperty("exceededTransferLimit")]
    [System.Text.Json.Serialization.JsonPropertyName("exceededTransferLimit")]
    public bool ExceededTransferLimit { get; set; }
}
