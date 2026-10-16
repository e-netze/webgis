#nullable enable

using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

/// <summary>
/// Editor for a user-defined layer labeling ("Beschriftung").
/// The current labeling is not sent by the server: the client keeps it at the service
/// (service.getLayerLabeling) and the plugin reads it from there.
/// </summary>
public class UILabelEditor : UICollapsableElement
{
    private readonly List<FieldDef> _fieldDefs = new List<FieldDef>();

    public UILabelEditor()
        : base("label-editor")
    {
    }

    [JsonProperty("field_defs")]
    [System.Text.Json.Serialization.JsonPropertyName("field_defs")]
    public IEnumerable<object> FieldDefs => _fieldDefs;

    [JsonProperty("service_id")]
    [System.Text.Json.Serialization.JsonPropertyName("service_id")]
    public string ServiceId { get; set; } = "";

    [JsonProperty("layer_id")]
    [System.Text.Json.Serialization.JsonPropertyName("layer_id")]
    public string LayerId { get; set; } = "";

    [JsonProperty("max_expression_length")]
    [System.Text.Json.Serialization.JsonPropertyName("max_expression_length")]
    public int MaxExpressionLength { get; set; }

    [JsonProperty("max_expression_lines")]
    [System.Text.Json.Serialization.JsonPropertyName("max_expression_lines")]
    public int MaxExpressionLines { get; set; }

    public void TryAddField(string fieldName, string? alias = null)
    {
        if (string.IsNullOrEmpty(fieldName) || _fieldDefs.Any(f => fieldName.Equals(f.Name)))
        {
            return;
        }

        _fieldDefs.Add(new FieldDef()
        {
            Name = fieldName,
            Aliasname = string.IsNullOrEmpty(alias) ? null : alias
        });
    }

    private class FieldDef
    {
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonProperty("alias", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("alias")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Aliasname { get; set; }
    }
}
