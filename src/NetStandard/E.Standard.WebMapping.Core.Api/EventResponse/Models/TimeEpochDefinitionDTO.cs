using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

public class TimeEpochDefinitionDTO
{
    [JsonProperty(PropertyName = "serviceId")]
    [System.Text.Json.Serialization.JsonPropertyName("serviceId")]
    public string ServiceId { get; set; }

    [JsonProperty(PropertyName = "epoch")]
    [System.Text.Json.Serialization.JsonPropertyName("epoch")]
    public long[] Epoch {  get; set; }
}