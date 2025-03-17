using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Features;
using Newtonsoft.Json;
using System.Dynamic;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

class JsonFeatureResponse
{
    [JsonProperty("displayFieldName")]
    [System.Text.Json.Serialization.JsonPropertyName("displayFieldName")]
    public string DisplayFieldName { get; set; }

    [JsonProperty("fieldAliases")]
    [System.Text.Json.Serialization.JsonPropertyName("fieldAliases")]
    public ExpandoObject FieldAliases { get; set; }

    [JsonProperty("geometryType")]
    [System.Text.Json.Serialization.JsonPropertyName("geometryType")]
    public string GeometryType { get; set; }

    [JsonProperty("spatialReference")]
    [System.Text.Json.Serialization.JsonPropertyName("spatialReference")]
    public JsonSpatialReference SpatialReference { get; set; }

    [JsonProperty("hasM")]
    [System.Text.Json.Serialization.JsonPropertyName("hasM")]
    public bool HasM { get; set; }

    [JsonProperty("hasZ")]
    [System.Text.Json.Serialization.JsonPropertyName("hasZ")]
    public bool HasZ { get; set; }

    [JsonProperty("fields")]
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public Field[] Fields { get; set; }

    [JsonProperty("features")]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    public JsonFeature[] Features { get; set; }

    [JsonProperty("exceededTransferLimit")]
    [System.Text.Json.Serialization.JsonPropertyName("exceededTransferLimit")]
    public bool ExceededTransferLimit { get; set; }

    #region Classes

    public class Field
    {
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonProperty("alias")]
        [System.Text.Json.Serialization.JsonPropertyName("alias")]
        public string Alias { get; set; }

        [JsonProperty("length")]
        [System.Text.Json.Serialization.JsonPropertyName("length")]
        public int? Length { get; set; }

        //public class VType
        //{
        //    [JsonProperty("value")]
        //    [System.Text.Json.Serialization.JsonPropertyName("value")]
        //    public string Value { get; set; }
        //}
    }

    #endregion
}
