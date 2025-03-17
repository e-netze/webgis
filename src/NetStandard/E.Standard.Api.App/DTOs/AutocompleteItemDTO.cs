using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Models;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AutocompleteItemDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GeoReferenceItemDTO))]
public class AutocompleteItemDTO
{
    public AutocompleteItemDTO()
    {

    }

    public AutocompleteItemDTO(SearchServiceItem item)
    {
        this.Id = item.Id;
        this.Label = this.Value = item.SuggestText;
        this.SubText = item.Subtext;
        this.Thumbnail = item.ThumbnailUrl;
        this.Link = item.Link;
        if (item.Geometry is Point)
        {
            this.Coords = new double[] { ((Point)item.Geometry).X, ((Point)item.Geometry).Y };
        }
        this.BBox = item.BBox;

        if (item.DoYouMean == true)
        {
            this.DoYouMean = true;
        }
    }

    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "label")]
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonProperty(PropertyName = "value")]
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonProperty(PropertyName = "link")]
    [System.Text.Json.Serialization.JsonPropertyName("link")]
    public string Link { get; set; }

    [JsonProperty(PropertyName = "subtext", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("subtext")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string SubText { get; set; }

    [JsonProperty(PropertyName = "thumbnail", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("thumbnail")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Thumbnail { get; set; }

    [JsonProperty(PropertyName = "coords", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("coords")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double[] Coords { get; set; }

    [JsonProperty(PropertyName = "bbox", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("bbox")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public double[] BBox
    { get; set; }

    [JsonProperty(PropertyName = "do_you_mean", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("do_you_mean")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? DoYouMean { get; set; }
}

public sealed class AutocomplteItemMetadata
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "type_name")]
    [System.Text.Json.Serialization.JsonPropertyName("type_name")]
    public string TypeName { get; set; }

    [JsonProperty(PropertyName = "sample")]
    [System.Text.Json.Serialization.JsonPropertyName("sample")]
    public string Sample { get; set; }

    [JsonProperty(PropertyName = "sample_separator", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("sample_separator")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string SampleSeparator { get; set; }

    [JsonProperty(PropertyName = "link")]
    [System.Text.Json.Serialization.JsonPropertyName("link")]
    public string Link { get; set; }

    [JsonProperty(PropertyName = "description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonProperty(PropertyName = "copyright_info", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("copyright_info")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public CopyrightInfoDTO CopyrightInfo { get; set; }
}