using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonFeatureField
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

    [JsonProperty("editable")]
    [System.Text.Json.Serialization.JsonPropertyName("editable")]
    public bool Editable { get; set; }

    [JsonProperty("nullable")]
    [System.Text.Json.Serialization.JsonPropertyName("nullable")]
    public bool Nullable { get; set; }

    [JsonProperty("length")]
    [System.Text.Json.Serialization.JsonPropertyName("length")]
    public int Length { get; set; }
}
