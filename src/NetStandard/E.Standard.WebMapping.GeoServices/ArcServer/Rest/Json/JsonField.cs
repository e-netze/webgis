using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonField
{
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonProperty("alias")]
    [System.Text.Json.Serialization.JsonPropertyName("alias")]
    public string Alias { get; set; }

    [JsonProperty("domain")]
    [System.Text.Json.Serialization.JsonPropertyName("domain")]
    public JsonDomain Domain { get; set; }
}
