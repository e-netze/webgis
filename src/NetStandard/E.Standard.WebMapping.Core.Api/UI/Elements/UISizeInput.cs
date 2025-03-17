using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UISizeInput : UIElement, IUIElementReadonly
{
    public UISizeInput(bool readOnly = false)
        : base("input-size")
    {
        this.@readonly = readOnly;
    }

    //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    //public int[] size { get; set; }

    public bool @readonly { get; set; }
}
