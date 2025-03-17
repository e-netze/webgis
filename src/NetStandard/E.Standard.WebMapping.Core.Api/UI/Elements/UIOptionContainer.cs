using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIOptionContainer : UICollapsableElement
{
    public UIOptionContainer()
        : base("optionscontainer")
    {

    }

    [JsonProperty("allow_null_values", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("allow_null_values")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowNullValues { get; set; }
}
