using Newtonsoft.Json;
using System.Collections.Generic;

namespace Api.Core.Models.DataLinq;

public class ApiFeatureCollection
{
    public ApiFeatureCollection()
    {
        this.Success = true;
    }

    [JsonProperty(PropertyName = "success")]
    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonProperty(PropertyName = "exception")]
    [System.Text.Json.Serialization.JsonPropertyName("exception")]
    public string Exception { get; set; }

    [JsonProperty(PropertyName = "features")]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    public Feature[] Features { get; set; }
    public class Feature
    {
        [JsonProperty(PropertyName = "oid")]
        [System.Text.Json.Serialization.JsonPropertyName("oid")]
        public string Oid { get; set; }

        [JsonProperty(PropertyName = "geometry")]
        [System.Text.Json.Serialization.JsonPropertyName("geometry")]
        public Geometry Geo { get; set; }

        [JsonProperty(PropertyName = "properties")]
        [System.Text.Json.Serialization.JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; }

        [JsonProperty(PropertyName = "bounds", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("bounds")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public double[] Bounds { get; set; }

        public class Geometry
        {
            [JsonProperty(PropertyName = "type")]
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string TypeName { get; set; }

            [JsonProperty(PropertyName = "coordinates")]
            [System.Text.Json.Serialization.JsonPropertyName("coordinates")]
            public double[] Coordinates { get; set; }
        }
    }
}
