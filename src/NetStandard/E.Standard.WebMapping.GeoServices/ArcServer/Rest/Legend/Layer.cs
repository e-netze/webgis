using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Legend;

class Layer
{
    [JsonProperty("layerId")]
    [System.Text.Json.Serialization.JsonPropertyName("layerId")]
    public int LayerId { get; set; }

    [JsonProperty("layerName")]
    [System.Text.Json.Serialization.JsonPropertyName("layerName")]
    public string LayerName { get; set; }

    [JsonProperty("layerType")]
    [System.Text.Json.Serialization.JsonPropertyName("layerType")]
    public string LayerType { get; set; }

    [JsonProperty("minScale")]
    [System.Text.Json.Serialization.JsonPropertyName("minScale")]
    public int MinScale { get; set; }

    [JsonProperty("maxScale")]
    [System.Text.Json.Serialization.JsonPropertyName("maxScale")]
    public int MaxScale { get; set; }

    [JsonProperty("legend")]
    [System.Text.Json.Serialization.JsonPropertyName("legend")]
    public Legend[] Legend { get; set; }
}
