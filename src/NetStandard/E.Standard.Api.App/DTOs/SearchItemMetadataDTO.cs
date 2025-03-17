using E.Standard.WebMapping.Core.Models;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs;

public sealed class SearchItemMetadataDTO
{
    public SearchItemMetadataDTO()
    {

    }

    public SearchItemMetadataDTO(SearchTypeMetadata metadata)
    {
        this.Id = metadata.Id;
        this.TypeName = metadata.TypeName;
        this.Sample = metadata.Sample;
        this.Description = metadata.Description;
        this.ServiceId = metadata.ServiceId;
        this.QueryId = metadata.QueryId;
    }


    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "type_name")]
    [System.Text.Json.Serialization.JsonPropertyName("type_name")]
    public string TypeName { get; set; }

    [JsonProperty(PropertyName = "sample", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("sample")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Sample { get; set; }

    [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Description { get; set; }

    [JsonProperty(PropertyName = "service", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("service")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string ServiceId { get; set; }

    [JsonProperty(PropertyName = "query", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("query")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string QueryId { get; set; }
}