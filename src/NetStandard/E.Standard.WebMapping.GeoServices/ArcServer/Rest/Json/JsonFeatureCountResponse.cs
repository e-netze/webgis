using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

class JsonFeatureCountResponse
{
    [JsonProperty("count")]
    [System.Text.Json.Serialization.JsonPropertyName("count")]
    public int Count { get; set; }
}
