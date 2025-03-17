using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UIImage : UIElement
{
    public UIImage(string src, bool isBase64 = false)
        : base("image")
    {
        if (isBase64)
        {
            this.Source = $"data:image/png;base64, {src}";
        }
        else
        {
            this.Source = src;
        }
    }

    [JsonProperty("src")]
    [System.Text.Json.Serialization.JsonPropertyName("src")]
    public string Source { get; set; }
}
