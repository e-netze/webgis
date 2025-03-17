using Newtonsoft.Json;

namespace E.Standard.CMS.UI.Controls;

public class SecurityButton : Control
{
    public SecurityButton(string path, string tagName, string name = "") : base(name)
    {
        this.Path = path;
        this.TagName = tagName;
    }

    [JsonProperty(PropertyName = "path")]
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonProperty(PropertyName = "authTag")]
    [System.Text.Json.Serialization.JsonPropertyName("authTag")]
    public string TagName { get; set; }
}
