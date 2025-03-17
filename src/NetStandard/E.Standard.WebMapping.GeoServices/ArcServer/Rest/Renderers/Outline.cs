using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers;

class Outline
{
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } // optional - one use is a Simple Fill Symbol Renderer

    [JsonProperty("style")]
    [System.Text.Json.Serialization.JsonPropertyName("style")]
    public string Style { get; set; } // optional - one use is a Simple Fill Symbol Renderer

    [JsonProperty("color")]
    [System.Text.Json.Serialization.JsonPropertyName("color")]
    public int[] Color { get; set; }

    [JsonProperty("width")]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public float Width { get; set; }
}
