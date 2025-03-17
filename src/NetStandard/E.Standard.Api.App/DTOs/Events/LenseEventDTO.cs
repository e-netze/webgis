using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class LenseEventDTO
{
    [JsonProperty(PropertyName = "width")]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public double Width { get; set; }
    [JsonProperty(PropertyName = "height")]
    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public double Height { get; set; }
    [JsonProperty(PropertyName = "options")]
    [System.Text.Json.Serialization.JsonPropertyName("options")]
    public LenseOptions Options { get; set; }

    public class LenseOptions
    {
        [JsonProperty(PropertyName = "zoom")]
        [System.Text.Json.Serialization.JsonPropertyName("zoom")]
        public bool Zoom { get; set; }

        [JsonProperty(PropertyName = "scalecontrolid")]
        [System.Text.Json.Serialization.JsonPropertyName("scalecontrolid")]
        public string ScaleControlId { get; set; }

        [JsonProperty(PropertyName = "lenseScale", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("lenseScale")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double? LenseScale { get; set; }
    }
}