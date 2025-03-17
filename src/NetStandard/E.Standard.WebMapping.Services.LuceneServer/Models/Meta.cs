using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.LuceneServer.Models;

public class Meta
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "category")]
    [System.Text.Json.Serialization.JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonProperty(PropertyName = "sample")]
    [System.Text.Json.Serialization.JsonPropertyName("sample")]
    public string Sample { get; set; }

    [JsonProperty(PropertyName = "description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Descrption { get; set; }

    [JsonProperty(PropertyName = "service")]
    [System.Text.Json.Serialization.JsonPropertyName("service")]
    public string Service { get; set; }

    [JsonProperty(PropertyName = "query")]
    [System.Text.Json.Serialization.JsonPropertyName("query")]
    public string Query { get; set; }
}
