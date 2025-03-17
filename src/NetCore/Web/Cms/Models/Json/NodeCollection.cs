using Newtonsoft.Json;
using System.Collections.Generic;

namespace Cms.Models.Json;

public class NodeCollection
{
    [JsonProperty(PropertyName = "nodes")]
    [System.Text.Json.Serialization.JsonPropertyName("nodes")]
    public IEnumerable<Node> Nodes { get; set; }

    [JsonProperty(PropertyName = "orderable")]
    [System.Text.Json.Serialization.JsonPropertyName("orderable")]
    public bool Orderable { get; set; }

    [JsonProperty(PropertyName = "navItems")]
    [System.Text.Json.Serialization.JsonPropertyName("navItems")]
    public IEnumerable<NavItem> NavItems { get; set; }

    [JsonProperty(PropertyName = "nodeTools")]
    [System.Text.Json.Serialization.JsonPropertyName("nodeTools")]
    public IEnumerable<NodeTool> NodeTools { get; set; }

    [JsonProperty(PropertyName = "path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonProperty(PropertyName = "description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; }
}
