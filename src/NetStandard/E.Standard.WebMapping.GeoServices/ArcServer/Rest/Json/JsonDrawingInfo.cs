using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonDrawingInfo
{
    [JsonProperty(PropertyName = "renderer")]
    [System.Text.Json.Serialization.JsonPropertyName("renderer")]
    public JsonRenderer Renderer
    {
        get; set;
    }

    #region Classes

    public class JsonRenderer
    {
        [JsonProperty(PropertyName = "type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "field1")]
        [System.Text.Json.Serialization.JsonPropertyName("field1")]
        public string Field1 { get; set; }
        [JsonProperty(PropertyName = "field2")]
        [System.Text.Json.Serialization.JsonPropertyName("field2")]
        public string Field2 { get; set; }
        [JsonProperty(PropertyName = "field3")]
        [System.Text.Json.Serialization.JsonPropertyName("field3")]
        public string Field3 { get; set; }

        [JsonProperty(PropertyName = "fieldDelimiter")]
        [System.Text.Json.Serialization.JsonPropertyName("fieldDelimiter")]
        public string FieldDelimiter { get; set; }
    }

    #endregion
}
