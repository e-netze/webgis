using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Legend;

class LegendResponse
{
    [JsonProperty("layers")]
    [System.Text.Json.Serialization.JsonPropertyName("layers")]
    public Layer[] Layers { get; set; }
}
