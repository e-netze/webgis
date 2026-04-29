#nullable enable

using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.ApiResult;

public class DownloadDTO : ApiResultDTO
{
    [JsonProperty(PropertyName = "downloadid", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("downloadid")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? EncryptedFilename { get; set; } = null;

    [JsonProperty(PropertyName = "clipboard_data", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("clipboard_data")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? ClipboardData { get; set; } = null;

    [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Name { get; set; } = "";

    [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Description { get; set; } = "";
}
