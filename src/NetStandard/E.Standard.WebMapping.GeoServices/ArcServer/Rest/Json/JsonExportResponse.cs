using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonExportResponse
{
    [JsonProperty("href")]
    [System.Text.Json.Serialization.JsonPropertyName("href")]
    public string Href { get; set; }

    [JsonProperty("imageData")]
    [System.Text.Json.Serialization.JsonPropertyName("imageData")]
    public string ImageData { get; set; }

    [JsonProperty("contentType")]
    [System.Text.Json.Serialization.JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonProperty("width")]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonProperty("height")]
    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonProperty("extent")]
    [System.Text.Json.Serialization.JsonPropertyName("extent")]
    public JsonExtent Extent { get; set; }

    [JsonProperty("scale")]
    [System.Text.Json.Serialization.JsonPropertyName("scale")]
    public double Scale { get; set; }
}
