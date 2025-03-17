using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Core.Models;

public class LayerVisibilityDefinitionDTO
{
    [JsonProperty(PropertyName = "service")]
    [System.Text.Json.Serialization.JsonPropertyName("service")]
    public string ServiceId { get; set; }

    [JsonProperty(PropertyName = "visible")]
    [System.Text.Json.Serialization.JsonPropertyName("visible")]
    public bool Visible { get; set; }

    [JsonProperty(PropertyName = "layers")]
    [System.Text.Json.Serialization.JsonPropertyName("layers")]
    public IEnumerable<string> Layers { get; set; }
}
