using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIDatePicker : UIElement
{
    public UIDatePicker()
        : base("input-date")
    {
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string label { get; set; }

    public bool date_only { get; set; }
}
