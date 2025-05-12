using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UILabel : UIElement, IUIElementLabel
{
    public UILabel()
        : base("label")
    {
        label = String.Empty;
    }

    public string label { get; set; }

    [JsonProperty(PropertyName = "is_trusted", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("is_trusted")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsTrusted { get; set; } = null;    
}
