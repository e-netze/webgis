using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.GeoJson;

public class GeoJsonFeatures
{
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type => "FeatureCollection";

    [JsonProperty("features")]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    public GeoJsonFeature[] Features { get; set; }

    [JsonProperty("crs", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("crs")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public CrsClass Crs { get; set; }

    public class CrsClass
    {
        [JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonProperty("properties")]
        [System.Text.Json.Serialization.JsonPropertyName("properties")]
        public IDictionary<string, object> Properties { get; set; }
    }
}
