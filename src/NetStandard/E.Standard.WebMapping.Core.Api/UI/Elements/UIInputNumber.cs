using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIInputNumber : UIElement
{
    public UIInputNumber(bool readOnly = false)
        : base("input-number")
    {
        this.@readonly = readOnly;

        this.MinValue = 0.0;
        this.MaxValue = 100.0;
        this.StepWidth = 1.0;
    }

    //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    //public int[] size { get; set; }

    public bool @readonly { get; set; }

    [JsonProperty("minValue")]
    [System.Text.Json.Serialization.JsonPropertyName("minValue")]
    public double MinValue { get; set; }
    [JsonProperty("maxValue")]
    [System.Text.Json.Serialization.JsonPropertyName("maxValue")]
    public double MaxValue { get; set; }
    [JsonProperty("stepWidth")]
    [System.Text.Json.Serialization.JsonPropertyName("stepWidth")]
    public double StepWidth { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string placeholder { get; set; }
}
