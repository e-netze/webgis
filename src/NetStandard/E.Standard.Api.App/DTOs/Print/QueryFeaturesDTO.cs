using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.DTOs.Print;

public sealed class QueryFeaturesDTO
{
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonProperty("features")]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    public IEnumerable<QueryFeatureDTO> Features { get; set; }

    public E.Standard.WebMapping.Core.Collections.FeatureCollection ToFeatureCollecton()
    {
        return new WebMapping.Core.Collections.FeatureCollection(this.Features?.Select(f => f.ToFeature()));
    }
}
