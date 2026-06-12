#nullable enable

using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonDateFieldsTimeReference
{
    [JsonProperty("timeZone")]
    [System.Text.Json.Serialization.JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }

    [JsonProperty("respectsDaylightSaving")]
    [System.Text.Json.Serialization.JsonPropertyName("respectsDaylightSaving")]
    public bool RespectsDaylightSaving { get; set; }
}
