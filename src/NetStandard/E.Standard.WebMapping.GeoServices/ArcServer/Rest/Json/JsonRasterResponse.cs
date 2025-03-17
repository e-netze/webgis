using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Geometry;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

class JsonRasterResponse
{

    [JsonProperty("results")]
    [System.Text.Json.Serialization.JsonPropertyName("results")]
    public Result[] Results { get; set; }

    #region Classes

    public class Result
    {
        [JsonProperty("layerId")]
        [System.Text.Json.Serialization.JsonPropertyName("layerId")]
        public int LayerId { get; set; }

        [JsonProperty("layerName")]
        [System.Text.Json.Serialization.JsonPropertyName("layerName")]
        public string LayerName { get; set; }

        [JsonProperty("displayFieldName")]
        [System.Text.Json.Serialization.JsonPropertyName("displayFieldName")]
        public string DisplayFieldName { get; set; }

        [JsonProperty("attributes")]
        [System.Text.Json.Serialization.JsonPropertyName("attributes")]
        public object ResultAttributes { get; set; }

        [JsonProperty("geometry")]
        [System.Text.Json.Serialization.JsonPropertyName("geometry")]
        public JsonGeometry Geometry { get; set; }
    }

    #endregion
}
