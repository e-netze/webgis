using E.Standard.WebMapping.Core.Api.EventResponse.Abstraction;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs;

public sealed class ImageLocationResponseDTO : VersionDTO, IImageLocationResponse
{
    public string id { get; set; }
    public string url { get; set; }
    public string requestid { get; set; }

    public double[] extent { get; set; }

    public double scale { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Path { get; set; }

    [JsonProperty("exception", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("exception")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string ErrorMesssage { get; set; }
}