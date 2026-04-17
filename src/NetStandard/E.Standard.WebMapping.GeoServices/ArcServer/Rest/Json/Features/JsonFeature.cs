using System.Dynamic;

using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Geometry;

using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Features;

class JsonFeature
{
    [JsonProperty("attributes")]
    [System.Text.Json.Serialization.JsonPropertyName("attributes")]
    public ExpandoObject Attributes { get; set; }  // dynamic

    [JsonProperty("geometry")]
    [System.Text.Json.Serialization.JsonPropertyName("geometry")]
    public JsonGeometry Geometry { get; set; }
}
