using E.Standard.Api.App.Models.Abstractions;
using E.Standard.WebMapping.Core.Api.Bridge;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class LabelingDTO : VersionDTO, IHtml, ILabelingBridge
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "fields")]
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public Field[] Fields { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string LayerId { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, string> FieldAliases
    {
        get
        {
            var ret = new Dictionary<string, string>();
            if (this.Fields != null)
            {
                foreach (var field in this.Fields)
                {
                    ret.Add(field.Name, field.Alias);
                }
            }
            return ret;
        }
    }

    #region Classes

    public class Field
    {
        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "alias")]
        [System.Text.Json.Serialization.JsonPropertyName("alias")]
        public string Alias { get; set; }
    }

    #endregion

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToTable(
            new string[] { "Id", "Name" },
            new object[] { this.Id, this.Name }
        ));

        return sb.ToString();
    }

    #endregion
}