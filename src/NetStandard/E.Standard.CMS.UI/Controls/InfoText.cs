using Newtonsoft.Json;

namespace E.Standard.CMS.UI.Controls;

public class InfoText : Control
{
    public InfoText(string name = "")
        : base(name)
    {

    }

    [JsonProperty(PropertyName = "bgColor")]
    [System.Text.Json.Serialization.JsonPropertyName("bgColor")]
    public string BgColor { get; set; }

    [JsonProperty(PropertyName = "text")]
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string Text { get; set; }
}
