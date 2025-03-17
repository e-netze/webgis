using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.Api.App.DTOs;

public sealed class SnapSchemaDTO
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonProperty(PropertyName = "min_scale")]
    [System.Text.Json.Serialization.JsonPropertyName("min_scale")]
    public int MinScale { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<string> LayerIds { get; set; }
}