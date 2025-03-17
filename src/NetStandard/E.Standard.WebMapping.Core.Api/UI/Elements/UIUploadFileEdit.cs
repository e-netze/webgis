using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIUploadFileEdit : UIElement
{
    public UIUploadFileEdit(string editService, string editTheme, string editField)
        : base("upload-file-control")
    {
        this.EditService = editService;
        this.EditTheme = editTheme;
        this.EditField = editField;
    }

    [JsonProperty("edit_service")]
    [System.Text.Json.Serialization.JsonPropertyName("edit_service")]
    public string EditService { get; set; }

    [JsonProperty("edit_theme")]
    [System.Text.Json.Serialization.JsonPropertyName("edit_theme")]
    public string EditTheme { get; set; }

    [JsonProperty("field_name")]
    [System.Text.Json.Serialization.JsonPropertyName("field_name")]
    public string EditField { get; set; }
}
