using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.EventResponse.Models;

public class EditingThemeDefDTO
{
    public EditingThemeDefDTO(string id, string serviceId)
    {
        this.Id = id;
        this.ServiceId = serviceId;
    }

    [JsonProperty("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonProperty("serviceid")]
    [System.Text.Json.Serialization.JsonPropertyName("serviceid")]
    public string ServiceId { get; set; }
}
