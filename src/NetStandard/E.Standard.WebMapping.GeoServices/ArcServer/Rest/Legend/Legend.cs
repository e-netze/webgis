using E.Standard.Json.Converters;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Legend;

class Legend
{
    [JsonProperty("label")]
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonProperty("url")]
    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonProperty("imageData")]
    [System.Text.Json.Serialization.JsonPropertyName("imageData")]
    public string ImageData { get; set; }

    [JsonProperty("contentType")]
    [System.Text.Json.Serialization.JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonProperty("height")]
    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonProperty("width")]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonProperty("values")]
    [System.Text.Json.Serialization.JsonPropertyName("values")]
    [System.Text.Json.Serialization.JsonConverter(typeof(StringArrayConverter))]
    public string[] Values { get; set; }
}
