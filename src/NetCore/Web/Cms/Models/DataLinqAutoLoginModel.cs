using Newtonsoft.Json;

namespace Cms.Models;

public class DataLinqAutoLoginModel
{
    [JsonProperty(PropertyName = "autoLogin")]
    [System.Text.Json.Serialization.JsonPropertyName("autoLogin")]
    public string AutoLogin { get; set; }
}
