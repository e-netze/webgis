using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class PrintContentDTO
{
    [JsonProperty(PropertyName = "url")]
    [System.Text.Json.Serialization.JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "preview")]
    [System.Text.Json.Serialization.JsonPropertyName("preview")]
    public string preview { get; set; }

    [JsonProperty(PropertyName = "downloadid")]
    [System.Text.Json.Serialization.JsonPropertyName("downloadid")]
    public string DownloadId { get; set; }

    [JsonProperty(PropertyName = "length")]
    [System.Text.Json.Serialization.JsonPropertyName("length")]
    public int Length { get; set; }
}