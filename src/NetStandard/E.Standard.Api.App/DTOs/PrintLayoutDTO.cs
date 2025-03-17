using E.Standard.WebMapping.Core.Api.Bridge;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs;

public sealed class PrintLayoutDTO : IPrintLayoutBridge
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string LayoutFile { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string LayoutParameters { get; set; }
}