using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

class UIPrintCoordinateLabelFieldCombo : UIElement
{
    public UIPrintCoordinateLabelFieldCombo()
        : base("print-coordinates-labelfield-combo")
    {
    }

    [JsonProperty("showCoordinatePairsOnly")]
    [System.Text.Json.Serialization.JsonPropertyName("showCoordinatePairsOnly")]
    public bool ShowCoordinatePairsOnly { get; set; }
}
