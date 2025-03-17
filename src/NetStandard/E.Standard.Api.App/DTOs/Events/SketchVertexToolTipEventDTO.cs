using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class SketchVertexToolTipEventDTO
{
    [JsonProperty(PropertyName = "x")]
    [System.Text.Json.Serialization.JsonPropertyName("x")]
    public double Longitude { get; set; }

    [JsonProperty(PropertyName = "y")]
    [System.Text.Json.Serialization.JsonPropertyName("y")]
    public double Latitude { get; set; }

    [JsonProperty(PropertyName = "tooltip")]
    [System.Text.Json.Serialization.JsonPropertyName("tooltip")]
    public string Tooltip { get; set; }
}