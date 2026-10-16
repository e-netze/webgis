#nullable enable

namespace E.Standard.WebGIS.Tools.Presentation.Models;

internal class TocUserRendererRequestDTO
{
    [Newtonsoft.Json.JsonProperty("serviceId")]
    [System.Text.Json.Serialization.JsonPropertyName("serviceId")]
    public string ServiceId { get; set; } = "";

    [Newtonsoft.Json.JsonProperty("layerId")]
    [System.Text.Json.Serialization.JsonPropertyName("layerId")]
    public string LayerId { get; set; } = "";

    [Newtonsoft.Json.JsonProperty("field")]
    [System.Text.Json.Serialization.JsonPropertyName("field")]
    public string? Field { get; set; }
}
