#nullable enable

using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Geometry;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.FeatureServer;

public class EsriFeature
{
    public EsriFeature()
    {
        Attributes = new Dictionary<string, object?>();
    }

    [JsonProperty(PropertyName = "attributes", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("attributes")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?> Attributes { get; set; }

    [JsonProperty(PropertyName = "geometry", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("geometry")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonGeometry? Geometry { get; set; }

    public EsriFeature Clone()
    {
        var clone = new EsriFeature()
        {
            Attributes = new Dictionary<string, object?>(this.Attributes),
            Geometry = this.Geometry
        };
        return clone;
    }
}
