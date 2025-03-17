using E.Standard.WebMapping.Core.Api.UI;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Events;

public sealed class NamedSketchDTO
{
    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty("subtext")]
    [System.Text.Json.Serialization.JsonPropertyName("subtext")]
    public string SubText { get; set; }

    [JsonProperty("sketch")]
    [System.Text.Json.Serialization.JsonPropertyName("sketch")]
    public E.Standard.Api.App.DTOs.Geometry.GeometryDTO Sketch { get; set; }

    [JsonProperty("zoom_on_preview")]
    [System.Text.Json.Serialization.JsonPropertyName("zoom_on_preview")]
    public bool ZoomOnPreview { get; set; }

    [JsonProperty("set_sketch")]
    [System.Text.Json.Serialization.JsonPropertyName("set_sketch")]
    public bool SetSketch { get; set; }

    [JsonProperty("setters", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("setters")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IUISetter[] UISetters { get; set; }
}