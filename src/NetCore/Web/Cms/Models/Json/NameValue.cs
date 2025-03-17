using E.Standard.Json.Converters;
using Newtonsoft.Json;

namespace Cms.Models.Json;

public class NameValue
{
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "value")]
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    [System.Text.Json.Serialization.JsonConverter(typeof(StringConverter))]
    public string Value { get; set; }
}
