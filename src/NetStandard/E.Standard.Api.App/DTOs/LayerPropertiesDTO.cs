using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Abstraction;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs;

public sealed class LayerPropertiesDTO : ILayerProperties
{
    [JsonProperty("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty("aliasname")]
    [System.Text.Json.Serialization.JsonPropertyName("aliasname")]
    public string Aliasname { get; set; }
    [JsonProperty("metadata")]
    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public string Metadata { get; set; }
    [JsonProperty("metadata_title")]
    [System.Text.Json.Serialization.JsonPropertyName("metadata_title")]
    public string MetadataTitle { get; set; }
    [JsonProperty("metadata_format")]
    [System.Text.Json.Serialization.JsonPropertyName("metadata_format")]
    public string MetadataFormat { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public BrowserWindowTarget2 MetadataTarget { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public MetadataButtonStyle MetadataButtonStyle { get; set; }
    [JsonProperty("ogcid")]
    [System.Text.Json.Serialization.JsonPropertyName("ogcid")]
    public string OgcId { get; set; }
    [JsonProperty("abstract")]
    [System.Text.Json.Serialization.JsonPropertyName("abstract")]
    public string Abstract { get; set; }

    [JsonProperty("legend_aliasname")]
    [System.Text.Json.Serialization.JsonPropertyName("legend_aliasname")]
    public string LegendAliasname { get; set; }

    [JsonProperty("visible")]
    [System.Text.Json.Serialization.JsonPropertyName("visible")]
    public bool Visible { get; set; }
    [JsonProperty("locked")]
    [System.Text.Json.Serialization.JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonProperty("show_in_legend")]
    [System.Text.Json.Serialization.JsonPropertyName("show_in_legend")]
    public bool ShowInLegend { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Name { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsTocLayer { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string OgcGroupId { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string OgcGroupTitle { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string UnDropdownableParentName { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string Description { get; set; }
}
