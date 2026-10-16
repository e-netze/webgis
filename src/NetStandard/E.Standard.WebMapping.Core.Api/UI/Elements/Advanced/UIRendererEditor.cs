#nullable enable

using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

/// <summary>
/// Editor for a user-defined layer renderer (simple / unique value).
/// The current renderer is not sent by the server: the client keeps it at the service
/// (service.getLayerRenderer) and the plugin reads it from there.
/// </summary>
public class UIRendererEditor : UICollapsableElement
{
    private readonly List<FieldDef> _fieldDefs = new List<FieldDef>();

    public UIRendererEditor()
        : base("renderer-editor")
    {
    }

    [JsonProperty("field_defs")]
    [System.Text.Json.Serialization.JsonPropertyName("field_defs")]
    public IEnumerable<object> FieldDefs => _fieldDefs;

    [JsonProperty("geometry_type")]
    [System.Text.Json.Serialization.JsonPropertyName("geometry_type")]
    public string GeometryType { get; set; } = "";

    [JsonProperty("service_id")]
    [System.Text.Json.Serialization.JsonPropertyName("service_id")]
    public string ServiceId { get; set; } = "";

    [JsonProperty("layer_id")]
    [System.Text.Json.Serialization.JsonPropertyName("layer_id")]
    public string LayerId { get; set; } = "";

    [JsonProperty("max_classes")]
    [System.Text.Json.Serialization.JsonPropertyName("max_classes")]
    public int MaxClasses { get; set; }

    [JsonProperty("callback_tool_id", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("callback_tool_id")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? CallbackToolId { get; set; }

    public void TryAddField(string fieldName, FieldType fieldType = FieldType.Unknown, string? alias = null)
    {
        if (string.IsNullOrEmpty(fieldName) || _fieldDefs.Any(f => fieldName.Equals(f.Name)))
        {
            return;
        }

        _fieldDefs.Add(new FieldDef()
        {
            Name = fieldName,
            Aliasname = string.IsNullOrEmpty(alias) ? null : alias,
            IsNumeric = E.Standard.WebMapping.Core.Renderer.UserRenderers.UserRendererValidator.IsNumericFieldType(fieldType)
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

        [JsonProperty("numeric")]
        [System.Text.Json.Serialization.JsonPropertyName("numeric")]
        public bool IsNumeric { get; set; }
    }
}
