using Newtonsoft.Json;

namespace Cms.Models.Json;

public class NavItem
{
    public NavItem() { }
    public NavItem(string name, string path, bool? obsolete = null)
    {
        this.Name = name;
        this.Path = path;
    }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonProperty(PropertyName = "obsolete", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("obsolete")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? Obsolete { get; set; }
}
