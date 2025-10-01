using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIInputText : UIValidation, IUIElementLabel, IUIElementReadonly
{
    public UIInputText() : this(false)
    {
    }

    public UIInputText(bool readOnly)
        : base("input-text")
    {
        this.@readonly = readOnly;
    }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string label { get; set; }

    public bool @readonly { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string placeholder { get; set; }
}
