using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers;

class Font
{
    [JsonProperty("family")]
    [System.Text.Json.Serialization.JsonPropertyName("family")]
    public string Family { get; set; }

    [JsonProperty("size")]
    [System.Text.Json.Serialization.JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonProperty("style")]
    [System.Text.Json.Serialization.JsonPropertyName("style")]
    public string Style { get; set; }

    [JsonProperty("weight")]
    [System.Text.Json.Serialization.JsonPropertyName("weight")]
    public string Weight { get; set; }

    [JsonProperty("decoration")]
    [System.Text.Json.Serialization.JsonPropertyName("decoration")]
    public string Decoration { get; set; }
}
