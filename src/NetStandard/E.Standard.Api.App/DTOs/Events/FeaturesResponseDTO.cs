using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class FeaturesResponseDTO : ToolEventResponseDTO
{
    public FeaturesDTO features { get; set; }
    public bool zoomtoresults { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string querytoolid { get; set; }
}