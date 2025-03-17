using Newtonsoft.Json;

namespace E.Standard.CMS.UI.Controls;

public class ControlLabel : Control
{
    public ControlLabel(string name) : base(name)
    {

    }

    [JsonProperty(PropertyName = "label")]
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public string Label { get; set; }
}
