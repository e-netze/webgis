using Newtonsoft.Json;

namespace Cms.Models.Json;

public class NodeTool
{
    [JsonProperty(PropertyName = "action")]
    [System.Text.Json.Serialization.JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "prompt")]
    [System.Text.Json.Serialization.JsonPropertyName("prompt")]
    public string Prompt { get; set; }

    [JsonProperty(PropertyName = "path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }
}
