using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Geometry;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

class JsonImageServerIdentifyResponse
{
    [JsonProperty("objectId")]
    [System.Text.Json.Serialization.JsonPropertyName("objectId")]
    public int ObjectId { get; set; }

    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonProperty("location")]
    [System.Text.Json.Serialization.JsonPropertyName("location")]
    public JsonGeometry Location { get; set; }
}
