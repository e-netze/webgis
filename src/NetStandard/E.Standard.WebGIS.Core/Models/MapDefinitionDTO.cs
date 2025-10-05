using Newtonsoft.Json;

namespace E.Standard.WebGIS.Core.Models;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(MapDefinitionDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(MapDefinitionUiDTO))]
public class MapDefinitionDTO
{
    [JsonProperty(PropertyName = "crs")]
    [System.Text.Json.Serialization.JsonPropertyName("crs")]
    public CrsDefinition Crs { get; set; }

    [JsonProperty(PropertyName = "scale")]
    [System.Text.Json.Serialization.JsonPropertyName("scale")]
    public double Scale { get; set; }

    [JsonProperty(PropertyName = "center")]
    [System.Text.Json.Serialization.JsonPropertyName("center")]
    public double[] Center { get; set; }

    [JsonProperty(PropertyName = "bounds")]
    [System.Text.Json.Serialization.JsonPropertyName("bounds")]
    public double[] Bounds { get; set; }

    [JsonProperty(PropertyName = "initialbounds")]
    [System.Text.Json.Serialization.JsonPropertyName("initialbounds")]
    public double[] InitialBounds { get; set; }

    [JsonProperty(PropertyName = "services")]
    [System.Text.Json.Serialization.JsonPropertyName("services")]
    public ServiceDefinition[] Services { get; set; }

    [JsonProperty(PropertyName = "selections")]
    [System.Text.Json.Serialization.JsonPropertyName("selections")]
    public SelectionDefinitionDTO[] Selections { get; set; }

    [JsonProperty(PropertyName = "focused_services")]
    [System.Text.Json.Serialization.JsonPropertyName("focused_services")]
    public FocusedServicesDefinitionDTO FocusedServices { get; set; }

    #region Classes

    public class CrsDefinition
    {
        [JsonProperty(PropertyName = "epsg")]
        [System.Text.Json.Serialization.JsonPropertyName("epsg")]
        public int Epsg { get; set; }

        [JsonProperty(PropertyName = "id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class ServiceDefinition
    {
        [JsonProperty(PropertyName = "id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "opacity")]
        [System.Text.Json.Serialization.JsonPropertyName("opacity")]
        public float Opacity { get; set; }

        [JsonProperty(PropertyName = "order")]
        [System.Text.Json.Serialization.JsonPropertyName("order")]
        public int? Order { get; set; }

        [JsonProperty(PropertyName = "layers")]
        [System.Text.Json.Serialization.JsonPropertyName("layers")]
        public LayerDefinition[] Layers { get; set; }

        [JsonProperty(PropertyName = "queries")]
        [System.Text.Json.Serialization.JsonPropertyName("queries")]
        public QueryDefinition[] Queries { get; set; }

        [JsonProperty(PropertyName = "time_epoch")]
        [System.Text.Json.Serialization.JsonPropertyName("time_epoch")]
        public TimeEpochDTO TimeEpoch { get; set; }

        #region Classes

        public class LayerDefinition
        {
            [JsonProperty(PropertyName = "id")]
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "name")]
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "visible")]
            [System.Text.Json.Serialization.JsonPropertyName("visible")]
            public bool Visible { get; set; }
        }

        public class LayerDefinitionForceVisibility : LayerDefinition
        {

        }

        public class QueryDefinition
        {
            [JsonProperty(PropertyName = "id")]
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "visible")]
            [System.Text.Json.Serialization.JsonPropertyName("visible")]
            public bool Visible { get; set; }
        }

        #endregion
    }

    public class SelectionDefinitionDTO
    {
        [JsonProperty(PropertyName = "type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "fids")]
        [System.Text.Json.Serialization.JsonPropertyName("fids")]
        public string FeatureIds { get; set; }

        [JsonProperty(PropertyName = "queryid")]
        [System.Text.Json.Serialization.JsonPropertyName("queryid")]
        public string QueryId { get; set; }

        [JsonProperty(PropertyName = "serviceid")]
        [System.Text.Json.Serialization.JsonPropertyName("serviceid")]
        public string ServiceId { get; set; }

        [JsonProperty(PropertyName = "customid")]
        [System.Text.Json.Serialization.JsonPropertyName("customid")]
        public string CustomId { get; set; }
    }

    public class FocusedServicesDefinitionDTO
    {
        [JsonProperty(PropertyName = "ids")]
        [System.Text.Json.Serialization.JsonPropertyName("ids")]
        public string[] Ids { get; set; }

        [JsonProperty(PropertyName = "opacity")]
        [System.Text.Json.Serialization.JsonPropertyName("opacity")]
        public float Opacity { get; set; }
    }

    public class TimeEpochDTO
    {
        [JsonProperty(PropertyName = "start")]
        [System.Text.Json.Serialization.JsonPropertyName("start")]
        public long Start { get; set; }

        [JsonProperty(PropertyName = "end")]
        [System.Text.Json.Serialization.JsonPropertyName("end")]
        public long End { get; set; }

        [JsonProperty(PropertyName = "relation")]
        [System.Text.Json.Serialization.JsonPropertyName("relation")]
        public string Relation { get; set; }
    }

    #endregion
}
