using Newtonsoft.Json;

namespace E.Standard.WebGIS.Core.Models;

public class PresentationDefintionDTO
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "service")]
    [System.Text.Json.Serialization.JsonPropertyName("service")]
    public string ServiceId { get; set; }

    [JsonProperty(PropertyName = "check")]
    [System.Text.Json.Serialization.JsonPropertyName("check")]
    public bool? Check { get; set; }
}
