#nullable enable

using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonAdvancedQueryCapabilities
{
    [JsonProperty("supportsPagination")]
    [System.Text.Json.Serialization.JsonPropertyName("supportsPagination")]
    public bool? SupportsPagination { get; set; }
}
