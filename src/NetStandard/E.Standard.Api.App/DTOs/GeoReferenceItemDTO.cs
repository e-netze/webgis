using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Models;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs;

public class GeoReferenceItemDTO : AutocompleteItemDTO, IGeoReferenceItem
{
    public GeoReferenceItemDTO()
        : base()
    {

    }

    public GeoReferenceItemDTO(SearchServiceItem item)
        : base(item)
    {
        this.Category = item.Category;
        this.Score = item.Score;
    }

    [JsonProperty(PropertyName = "category", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("category")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Category { get; set; }

    [JsonProperty(PropertyName = "score", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("score")]
    //[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double Score { get; set; }
}