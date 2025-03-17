using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Features;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

class JsonRasterAttributeTable
{
    [JsonProperty("fields")]
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public JsonRasterTableFields[] Fields { get; set; }

    [JsonProperty("features")]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    public RasterAttributeTableFeature[] Features { get; set; }
}
