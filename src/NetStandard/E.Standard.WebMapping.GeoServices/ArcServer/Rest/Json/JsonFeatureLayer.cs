using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonFeatureLayer
{
    public JsonFeatureLayer()
    {
        HasM = HasZ = false;
    }

    [JsonProperty("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonProperty("geometryType")]
    [System.Text.Json.Serialization.JsonPropertyName("geometryType")]
    public string GeometryType { get; set; }

    [JsonProperty("parentLayerId")]
    [System.Text.Json.Serialization.JsonPropertyName("parentLayerId")]
    public int ParentLayerId { get; set; }

    [JsonProperty("minScale")]
    [System.Text.Json.Serialization.JsonPropertyName("minScale")]
    public double MinScale { get; set; }

    [JsonProperty("maxScale")]
    [System.Text.Json.Serialization.JsonPropertyName("maxScale")]
    public double MaxScale { get; set; }

    [JsonProperty("defaultVisibility")]
    [System.Text.Json.Serialization.JsonPropertyName("defaultVisibility")]
    public bool DefaultVisibility { get; set; }

    [JsonProperty("fields")]
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public JsonFeatureField[] Fields { get; set; }

    [JsonProperty("extent")]
    [System.Text.Json.Serialization.JsonPropertyName("extent")]
    public JsonExtent Extent { get; set; }

    [JsonProperty("drawingInfo")]
    [System.Text.Json.Serialization.JsonPropertyName("drawingInfo")]
    public JsonDrawingInfo DrawingInfo { get; set; }

    [JsonProperty("hasZ")]
    [System.Text.Json.Serialization.JsonPropertyName("hasZ")]
    public bool HasZ { get; set; }

    [JsonProperty("hasM")]
    [System.Text.Json.Serialization.JsonPropertyName("hasM")]
    public bool HasM { get; set; }

    [JsonProperty("description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonProperty("capabilities")]
    [System.Text.Json.Serialization.JsonPropertyName("capabilities")]
    public string Capabilities { get; set; }
}
