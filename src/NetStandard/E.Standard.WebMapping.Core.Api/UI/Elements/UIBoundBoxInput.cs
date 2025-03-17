using E.Standard.WebMapping.Core.Api.UI.Abstractions;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIBoundBoxInput : UIElement, IUIElementReadonly
{
    public UIBoundBoxInput(bool readOnly = false)
        : base("input-bbox")
    {
        this.@readonly = readOnly;
    }

    //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    //[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    //public double[] bbox { get; set; }


    public bool @readonly { get; set; }
}
