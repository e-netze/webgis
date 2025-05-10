using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.FeatureServer;

public class EsriFeatures
{
    [JsonProperty(PropertyName = "features", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<EsriFeature> Features { get; set; }

    [JsonProperty("spatialReference", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("spatialReference")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonSpatialReference SpatialReference { get; set; }

}