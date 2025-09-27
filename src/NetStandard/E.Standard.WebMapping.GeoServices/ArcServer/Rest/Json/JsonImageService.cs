#nullable enable

using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

class JsonImageService
{
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonProperty(PropertyName = "serviceDescription")]
    [System.Text.Json.Serialization.JsonPropertyName("serviceDescription")]
    public string? ServiceDescription { get; set; }

    [JsonProperty(PropertyName = "description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonProperty(PropertyName = "copyrightText")]
    [System.Text.Json.Serialization.JsonPropertyName("copyrightText")]
    public string? CopyrightText { get; set; }

    [JsonProperty("spatialReference")]
    [System.Text.Json.Serialization.JsonPropertyName("spatialReference")]
    public JsonSpatialReference? SpatialReference { get; set; }

    [JsonProperty("timeInfo")]
    [System.Text.Json.Serialization.JsonPropertyName("timeInfo")]
    public JsonTimeInfo? TimeInfo { get; set; }
}
