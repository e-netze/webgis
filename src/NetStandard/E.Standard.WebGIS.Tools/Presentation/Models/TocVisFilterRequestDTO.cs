#nullable enable

using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Presentation.Models;

internal class TocVisFilterRequestDTO
{
    [Newtonsoft.Json.JsonProperty("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [Newtonsoft.Json.JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [Newtonsoft.Json.JsonProperty("serviceLayers")]
    [System.Text.Json.Serialization.JsonPropertyName("serviceLayers")]
    public Dictionary<string, string[]>? ServiceLayers { get; set; }
}
