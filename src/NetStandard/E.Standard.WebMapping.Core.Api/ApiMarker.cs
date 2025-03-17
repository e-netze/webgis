using E.Standard.Drawing.Models;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api;

public class ApiMarker
{
    public string ImageUrl { get; set; }
    public Dimension ImageSize { get; set; }
    public Position? Anchor { get; set; }
    public Position? PopupAnchor { get; set; }

    public object ToJsonMarker()
    {
        return new JsMarker()
        {
            IconUrl = this.ImageUrl,
            IconSize = new int[] { this.ImageSize.Width, this.ImageSize.Height },
            IconAnchor = this.Anchor.HasValue && (this.Anchor.Value.X != 0 || this.Anchor.Value.Y != 0)
                ? new[] { this.Anchor.Value.X, this.Anchor.Value.Y }
                : null,
            PopupAnchor = this.PopupAnchor.HasValue && (this.PopupAnchor.Value.X != 0 || this.PopupAnchor.Value.Y != 0)
                ? new[] { this.PopupAnchor.Value.X, this.PopupAnchor.Value.Y }
                : null,
        };
    }

    private class JsMarker
    {
        [JsonProperty(PropertyName = "iconUrl")]
        [System.Text.Json.Serialization.JsonPropertyName("iconUrl")]
        public string IconUrl { get; set; }

        [JsonProperty(PropertyName = "iconSize")]
        [System.Text.Json.Serialization.JsonPropertyName("iconSize")]
        public int[] IconSize { get; set; }

        [JsonProperty(PropertyName = "iconAnchor", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("iconAnchor")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int[] IconAnchor { get; set; }

        [JsonProperty(PropertyName = "popupAnchor", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("popupAnchor")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public int[] PopupAnchor { get; set; }
    }
}
