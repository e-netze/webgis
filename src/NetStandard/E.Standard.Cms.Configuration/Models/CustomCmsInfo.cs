using Newtonsoft.Json;

namespace E.Standard.Cms.Configuration.Models;

public class CustomCmsInfo
{
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("secrets-password")]
    [System.Text.Json.Serialization.JsonPropertyName("secrets-password")]
    public string SecretsPassword { get; set; }
}
