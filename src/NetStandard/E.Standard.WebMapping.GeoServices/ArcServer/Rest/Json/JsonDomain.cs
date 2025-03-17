using E.Standard.Json.Converters;
using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonDomain
{
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonProperty("codedValues")]
    [System.Text.Json.Serialization.JsonPropertyName("codedValues")]
    public JsonCodedValues[] CodedValues { get; set; }
}

public class JsonCodedValues
{
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    [System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
    public string Name { get; set; }
    [JsonProperty("code")]
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    [System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
    public string Code { get; set; }
}
