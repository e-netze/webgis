using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Print;

public sealed class CoordinateFeaturesDTO
{
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonProperty("features")]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    public IEnumerable<CoordinateFeatureDTO> Features { get; set; }

    public E.Standard.WebMapping.Core.Collections.FeatureCollection ToFeatureCollecton(string coordinateField)
    {
        return new WebMapping.Core.Collections.FeatureCollection(this.Features?.Select(f => f.ToFeature(coordinateField)));
    }
}
