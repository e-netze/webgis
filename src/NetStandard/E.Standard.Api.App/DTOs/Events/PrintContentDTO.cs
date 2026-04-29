using E.Standard.Api.App.DTOs.ApiResult;

using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class PrintContentDTO
{
    [JsonProperty(PropertyName = "url")]
    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "preview")]
    [System.Text.Json.Serialization.JsonPropertyName("preview")]
    public string Preview { get; set; }

    [JsonProperty(PropertyName = "downloadid", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("downloadid")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string EncryptedFilename { get; set; } = null;

    [JsonProperty(PropertyName = "length")]
    [System.Text.Json.Serialization.JsonPropertyName("length")]
    public int Length { get; set; }
}
