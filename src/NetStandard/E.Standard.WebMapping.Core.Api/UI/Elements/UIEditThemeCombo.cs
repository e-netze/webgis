using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIEditThemeCombo : UIElement
{
    public UIEditThemeCombo()
        : base("editthemecombo")
    {
    }

    public UINameValue[] customitems { get; set; }

    public string onchange { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string db_rights { get; set; }
}
