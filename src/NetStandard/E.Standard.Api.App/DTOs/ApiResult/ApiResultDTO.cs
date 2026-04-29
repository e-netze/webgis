using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.ApiResult;

public class ApiResultDTO
{
    [JsonProperty(PropertyName = "success")]
    [System.Text.Json.Serialization.JsonPropertyName("success")]
    public bool Success { get; set; } = true;
}
