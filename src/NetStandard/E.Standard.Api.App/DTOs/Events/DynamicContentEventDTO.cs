using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class DynamicContentEventDTO
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "url")]
    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }
}