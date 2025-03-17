using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Cms.Models.Json;

public class NavTreeNode
{
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "aliasname")]
    [System.Text.Json.Serialization.JsonPropertyName("aliasname")]
    public string AliasName { get; set; }

    [JsonProperty(PropertyName = "path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    private List<NavTreeNode> _nodes = null;
    [JsonProperty(PropertyName = "nodes", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("nodes")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<NavTreeNode> Nodes { get { return _nodes; } }

    [JsonProperty(PropertyName = "selectable", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("selectable")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? Selectable { get; set; }

    public void Add(NavTreeNode node)
    {
        if (_nodes == null)
        {
            _nodes = new List<NavTreeNode>();
        }

        for (int i = 0; i < _nodes.Count(); i++)
        {
            if (_nodes[i].AliasName.CompareTo(node.AliasName) > 0)
            {
                _nodes.Insert(i, node);
                return;
            }
        }

        _nodes.Add(node);
    }
}
