using Newtonsoft.Json;

namespace E.Standard.WebGIS.Core.Models;

public class MapMarkerDTO
{
    [JsonProperty(PropertyName = "x")]
    [System.Text.Json.Serialization.JsonPropertyName("x")]
    public double X { get; set; }

    [JsonProperty(PropertyName = "y")]
    [System.Text.Json.Serialization.JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonProperty(PropertyName = "lng")]
    [System.Text.Json.Serialization.JsonPropertyName("lng")]
    public double Lng { get; set; }

    [JsonProperty(PropertyName = "lat")]
    [System.Text.Json.Serialization.JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonProperty(PropertyName = "icon")]
    [System.Text.Json.Serialization.JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonProperty(PropertyName = "text")]
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonProperty(PropertyName = "openPopup")]
    [System.Text.Json.Serialization.JsonPropertyName("openPopup")]
    public bool OpenPopup { get; set; }

    [JsonProperty(PropertyName = "srs")]
    [System.Text.Json.Serialization.JsonPropertyName("srs")]
    public int Srs { get; set; }
}
