using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonFeatureServerLayer : JsonFeatureLayer
{
    public JsonFeatureServerLayer() : base()
    {
    }

    [JsonProperty("geometryField")]
    [System.Text.Json.Serialization.JsonPropertyName("geometryField")]
    public JsonFeatureField GeometryField { get; set; }
}
