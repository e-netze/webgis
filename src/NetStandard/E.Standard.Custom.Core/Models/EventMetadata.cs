using Newtonsoft.Json;

namespace E.Standard.Custom.Core.Models;

public class EventMetadata
{
    [JsonProperty("portal")]
    [System.Text.Json.Serialization.JsonPropertyName("portal")]
    public string Portal { get; set; }
    [JsonProperty("cat")]
    [System.Text.Json.Serialization.JsonPropertyName("cat")]
    public string Category { get; set; }
    [JsonProperty("map")]
    [System.Text.Json.Serialization.JsonPropertyName("map")]
    public string MapName { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Username { get; set; }
}
