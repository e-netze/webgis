using Newtonsoft.Json;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Models;

public class GeoPosition
{
    [JsonProperty(PropertyName = "epsg")]
    [System.Text.Json.Serialization.JsonPropertyName("epsg")]
    public int Epsg { get; set; }

    [JsonProperty(PropertyName = "x")]
    [System.Text.Json.Serialization.JsonPropertyName("x")]
    public double X { get; set; }
    [JsonProperty(PropertyName = "y")]
    [System.Text.Json.Serialization.JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonProperty(PropertyName = "lng")]
    [System.Text.Json.Serialization.JsonPropertyName("lng")]
    public double Latitude { get; set; }
    [JsonProperty(PropertyName = "lat")]
    [System.Text.Json.Serialization.JsonPropertyName("lat")]
    public double Longitude { get; set; }
}
