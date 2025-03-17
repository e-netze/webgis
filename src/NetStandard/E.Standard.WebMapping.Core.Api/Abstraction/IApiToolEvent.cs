using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.Abstraction;

public interface IApiToolEvent
{
    [JsonProperty("event")]
    [System.Text.Json.Serialization.JsonPropertyName("event")]
    ApiToolEvents Event { get; }

    [JsonProperty("command")]
    [System.Text.Json.Serialization.JsonPropertyName("command")]
    string ToolCommand { get; }
}
