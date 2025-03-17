using Newtonsoft.Json;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Models;

public class PassPoint
{
    [JsonProperty(PropertyName = "vector_x")]
    [System.Text.Json.Serialization.JsonPropertyName("vector_x")]
    public double VectorX { get; set; }

    [JsonProperty(PropertyName = "vector_y")]
    [System.Text.Json.Serialization.JsonPropertyName("vector_y")]
    public double VectorY { get; set; }

    [JsonProperty(PropertyName = "x")]
    [System.Text.Json.Serialization.JsonPropertyName("x")]
    public int ImageX { get; set; }
    [JsonProperty(PropertyName = "y")]
    [System.Text.Json.Serialization.JsonPropertyName("y")]
    public int ImageY { get; set; }

    [JsonProperty(PropertyName = "world_pos")]
    [System.Text.Json.Serialization.JsonPropertyName("world_pos")]
    public GeoPosition WorldPoint { get; set; }
}
