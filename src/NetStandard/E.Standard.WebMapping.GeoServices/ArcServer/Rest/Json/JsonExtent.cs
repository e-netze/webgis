using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonExtent
{
    [JsonProperty("xmin")]
    [System.Text.Json.Serialization.JsonPropertyName("xmin")]
    public double Xmin { get; set; }

    [JsonProperty("ymin")]
    [System.Text.Json.Serialization.JsonPropertyName("ymin")]
    public double Ymin { get; set; }

    [JsonProperty("xmax")]
    [System.Text.Json.Serialization.JsonPropertyName("xmax")]
    public double Xmax { get; set; }

    [JsonProperty("ymax")]
    [System.Text.Json.Serialization.JsonPropertyName("ymax")]
    public double Ymax { get; set; }

    [JsonProperty("spatialReference")]
    [System.Text.Json.Serialization.JsonPropertyName("spatialReference")]
    public JsonSpatialReference SpatialReference { get; set; }

    public bool IsInitialized()
    {
        return Xmin != 0D || Ymin != 0D || Xmax != 0D || Ymax != 0D;
    }
}
