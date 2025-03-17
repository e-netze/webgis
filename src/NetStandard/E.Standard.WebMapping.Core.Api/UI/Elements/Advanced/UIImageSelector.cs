using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UIImageSelector : UIElement
{
    public UIImageSelector()
        : base("image-selector")
    {
        MultiSelect = true;
    }

    [JsonProperty("image_urls")]
    [System.Text.Json.Serialization.JsonPropertyName("image_urls")]
    public IEnumerable<string> ImageUrls { get; set; }

    [JsonProperty("image_labels")]
    [System.Text.Json.Serialization.JsonPropertyName("image_labels")]
    public IEnumerable<string> ImageLabels { get; set; }

    [JsonProperty("image_width", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("image_width")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? ImageWidth { get; set; }

    [JsonProperty("image_height", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("image_height")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? ImageHeight { get; set; }

    [JsonProperty("multi_select")]
    [System.Text.Json.Serialization.JsonPropertyName("multi_select")]
    public bool MultiSelect { get; set; }
}
