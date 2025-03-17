using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonService
{
    [JsonProperty(PropertyName = "currentVersion")]
    [System.Text.Json.Serialization.JsonPropertyName("currentVersion")]
    public double CurrentVersion { get; set; }

    [JsonProperty(PropertyName = "serviceDescription")]
    [System.Text.Json.Serialization.JsonPropertyName("serviceDescription")]
    public string ServiceDescription { get; set; }

    [JsonProperty(PropertyName = "description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonProperty(PropertyName = "copyrightText")]
    [System.Text.Json.Serialization.JsonPropertyName("copyrightText")]
    public string CopyrightText { get; set; }

    [JsonProperty(PropertyName = "mapName")]
    [System.Text.Json.Serialization.JsonPropertyName("mapName")]
    public string MapName { get; set; }

    [JsonProperty("spatialReference")]
    [System.Text.Json.Serialization.JsonPropertyName("spatialReference")]
    public JsonSpatialReference SpatialReference { get; set; }

    [JsonProperty("maxImageWidth")]
    [System.Text.Json.Serialization.JsonPropertyName("maxImageWidth")]
    public int MaxImageWidth { get; set; }

    [JsonProperty("maxImageHeight")]
    [System.Text.Json.Serialization.JsonPropertyName("maxImageHeight")]
    public int MaxImageHeight { get; set; }

    [JsonProperty("maxRecordCount")]
    [System.Text.Json.Serialization.JsonPropertyName("maxRecordCount")]
    public int MaxRecordCount { get; set; }
}
