using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIClickToggleElement : UIElement
{
    public UIClickToggleElement(string toggleStyle, string toggleStyleValue, bool resetSiblings = false)
        : base("click-toggle")
    {
        this.ToggleStyle = toggleStyle;
        this.ToggleStyleValue = toggleStyleValue;
        this.ResetSiblings = resetSiblings;
    }

    [JsonProperty("togglestyle")]
    [System.Text.Json.Serialization.JsonPropertyName("togglestyle")]
    public string ToggleStyle { get; set; }

    [JsonProperty("togglestylevalue")]
    [System.Text.Json.Serialization.JsonPropertyName("togglestylevalue")]
    public string ToggleStyleValue { get; set; }

    [JsonProperty("resetsiblings")]
    [System.Text.Json.Serialization.JsonPropertyName("resetsiblings")]
    public bool ResetSiblings { get; set; } = false;
}
