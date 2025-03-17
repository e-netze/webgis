using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cms.Models.Json;

public class NodeProperties
{
    [JsonProperty(PropertyName = "displayName")]
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public string DisplayName { get; set; }

    [JsonProperty(PropertyName = "properties")]
    [System.Text.Json.Serialization.JsonPropertyName("properties")]
    public IEnumerable<NodeProperty> Properties { get; set; }

    [JsonProperty(PropertyName = "path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonProperty(PropertyName = "subProperty", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("subProperty")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string SubProperty { get; set; }

    [JsonProperty(PropertyName = "readonly", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("readonly")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsReadonly { get; set; }
}
