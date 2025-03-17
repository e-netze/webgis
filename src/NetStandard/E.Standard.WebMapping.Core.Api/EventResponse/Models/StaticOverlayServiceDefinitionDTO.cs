using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

public class StaticOverlayServiceDefinitionDTO
{
    public StaticOverlayServiceDefinitionDTO()
    {
        WidthHeightRatio = 1f;
    }

    [JsonProperty("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "overlay_url")]
    [System.Text.Json.Serialization.JsonPropertyName("overlay_url")]
    public string OverlayUrl { get; set; }

    [JsonProperty(PropertyName = "opacity", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("opacity")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double? Opacity { get; set; }

    [JsonProperty(PropertyName = "topLeft")]
    [System.Text.Json.Serialization.JsonPropertyName("topLeft")]
    public double[] TopLeft { get; set; }
    [JsonProperty(PropertyName = "topRight")]
    [System.Text.Json.Serialization.JsonPropertyName("topRight")]
    public double[] TopRight { get; set; }
    [JsonProperty(PropertyName = "bottomLeft")]
    [System.Text.Json.Serialization.JsonPropertyName("bottomLeft")]
    public double[] BottomLeft { get; set; }

    [JsonProperty(PropertyName = "passPoints", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("passPoints")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<PassPoint> PassPoints { get; set; }

    [JsonProperty("editmode")]
    [System.Text.Json.Serialization.JsonPropertyName("editmode")]
    public bool EditMode { get; set; }

    [JsonProperty(PropertyName = "widthHeightRatio")]
    [System.Text.Json.Serialization.JsonPropertyName("widthHeightRatio")]
    public float WidthHeightRatio { get; set; }

    #region Classes

    public class PassPoint
    {
        [JsonProperty("vec")]
        [System.Text.Json.Serialization.JsonPropertyName("vec")]
        public double[] Vector { get; set; }
        [JsonProperty("pos")]
        [System.Text.Json.Serialization.JsonPropertyName("pos")]
        public double[] World { get; set; }
    }

    #endregion
}
