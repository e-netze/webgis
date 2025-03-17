using E.Standard.Api.App.Models.Abstractions;
using Newtonsoft.Json;
using System;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class PresentationDTO : IHtml
{
    public string id { get; set; }
    public string name { get; set; }

    public string[] layers { get; set; }
    public string description { get; set; }
    public string thumbnail { get; set; }

    public bool basemap { get; set; }

    public GdiProperties[] items
    {
        get;
        set;
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty
    {
        get; set;
    }

    public PresentationDTO Clone()
    {
        return new PresentationDTO()
        {
            id = id,
            name = name,
            layers = layers,
            description = description,
            thumbnail = thumbnail,
            basemap = basemap,
            items = items,
            IsEmpty = IsEmpty
        };
    }

    #region Sub classes

    public class GdiProperties : IHtml
    {
        public string container { get; set; }

        public string name { get; set; }
        public string groupstyle { get; set; }
        public bool visible { get; set; }
        [JsonProperty("client_visibility", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("client_visibility")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string client_visibility { get; set; }
        public bool visible_with_service { get; set; }
        public string[] visible_with_one_of_services { get; set; }

        public string affecting { get; set; }
        public string style { get; set; }

        public int container_order { get; set; }
        public int group_order { get; set; }
        public int item_order { get; set; }

        public string metadata { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string metadata_target { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string metadata_title { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string metadata_button_style { get; set; }

        public string group_metadata { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string group_metadata_target { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string group_metadata_title { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string group_metadata_button_style { get; set; }

        //[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        //[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        //public bool? allow_as_dynamic_markers { get; set; }

        [JsonProperty("ui_groupname", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("ui_groupname")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string ui_groupname { get; set; }

        #region IHtml Member

        public string ToHtmlString()
        {
            return HtmlHelper.ToTable(
                new string[] { "Container", "Groupname", "Groupstyle", "Visible With Service", "Visible", "Visible With Services", "Affecting", "Style", "Metadata", "GroupMetadata", "index", "group_index" },
                new object[] { this.container,
                    this.name,
                    this.groupstyle,
                    this.visible,
                    this.visible_with_service,
                    this.visible_with_one_of_services!=null ? String.Join(", ", this.visible_with_one_of_services) : String.Empty,
                    this.affecting,
                    this.style,
                    this.metadata,
                    this.group_metadata,
                    this.item_order,
                    this.group_order
                }
                );
        }

        #endregion
    }

    #endregion

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToHeader(this.name, HtmlHelper.HeaderType.h2));

        sb.Append(HtmlHelper.ToTable(
            new string[] { "Id", "Name", "Layers", "Description", "Thumbnail", "Basemap" },
            new object[] { this.id, this.name, this.layers, this.description, this.thumbnail, this.basemap }
        ));

        sb.Append(HtmlHelper.ToList(this.items, "Items", HtmlHelper.HeaderType.h6));

        return sb.ToString();
    }

    #endregion
}