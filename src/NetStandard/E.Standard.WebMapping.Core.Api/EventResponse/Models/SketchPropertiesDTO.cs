using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

public class SketchPropertiesDTO
{
    [JsonProperty("element_width", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("element_width")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double? ElementWidth { get; set; }

    [JsonProperty("element_height", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("element_height")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double? ElementHeight { get; set; }   
}