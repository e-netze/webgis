using E.Standard.Api.App.Models.Abstractions;
using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Tools;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ToolDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ClientButtonToolDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ClientToolDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ServerButtonToolDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ServerToolDTO))]
public class ToolDTO : IHtml
{
    public string id { get; set; }
    public string name { get; set; }
    public string container { get; set; }
    public string image { get; set; }
    public string tooltip { get; set; }
    public bool hasui { get; set; }
    public string parentid { get; set; }

    [JsonProperty(PropertyName = "is_childtool", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("is_childtool")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? is_childtool { get; set; }

    [JsonProperty(PropertyName = "cursor", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("cursor")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string cursor { get; set; }

    [JsonProperty(PropertyName = "dependencies", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("dependencies")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] dependencies { get; set; }

    [JsonProperty(PropertyName = "confirmmessages", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("confirmmessages")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ToolConfirmMessageDTO[] confirmmessages { get; set; }

    [JsonProperty(PropertyName = "persistencecontext", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("persistencecontext")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string persistencecontext { get; set; }

    [JsonProperty(PropertyName = "marker", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("marker")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object marker { get; set; }

    [JsonProperty(PropertyName = "event_handlers", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("event_handlers")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] EventHandlers { get; set; }

    [JsonProperty(PropertyName = "o")]
    [System.Text.Json.Serialization.JsonPropertyName("o")]
    public string[] OnButtonClickDependencies { get; set; }

    [JsonProperty(PropertyName = "max_sketch_vertices", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("max_sketch_vertices")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxSketchVertices { get; set; }

    [JsonProperty(PropertyName = "visfilter_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("visfilter_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? VisFilterDependent { get; set; }

    [JsonProperty(PropertyName = "labeling_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("labeling_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? LabelingDependent { get; set; }

    [JsonProperty(PropertyName = "allow_ctrlbbox", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("allow_ctrlbbox")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? AllowCtrlBBox { get; set; }

    [JsonProperty(PropertyName = "selectioninfo_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("selectioninfo_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? SelectionInfoDependent { get; set; }

    [JsonProperty(PropertyName = "scale_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("scale_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? ScaleDependent { get; set; }

    [JsonProperty(PropertyName = "mapcrs_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("mapcrs_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? MapCrsDependent { get; set; }

    [JsonProperty(PropertyName = "mapbbox_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("mapbbox_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? MapBBoxDependent { get; set; }

    [JsonProperty(PropertyName = "printlayout_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("printlayout_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? PrintLayoutRotationDependent { get; set; }

    [JsonProperty(PropertyName = "mapimagesize_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("mapimagesize_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? MapImageSizeDependent { get; set; }

    [JsonProperty(PropertyName = "device_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("device_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? ClientDeviceDependent { get; set; }

    [JsonProperty(PropertyName = "anonymous_userid_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("anonymous_userid_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? AnonymousUserIdDependent { get; set; }

    [JsonProperty(PropertyName = "aside_dialog_exists_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("aside_dialog_exists_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? AsideDialogExistsDependent { get; set; }

    [JsonProperty(PropertyName = "liveshare_clientname_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("liveshare_clientname_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? LiveShareClientnameDependent { get; set; }

    [JsonProperty(PropertyName = "custom_service_reqeuest_parameters_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("custom_service_reqeuest_parameters_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? CustomServiceRequestParametersDependent { get; set; }

    [JsonProperty(PropertyName = "query_markers_visiblity_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("query_markers_visiblity_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? QueryMarkersVisibilityDependent { get; set; }

    [JsonProperty(PropertyName = "coordinate_markers_visiblity_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("coordinate_markers_visiblity_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? CoordinateMarkersVisibilityDependent { get; set; }

    [JsonProperty(PropertyName = "chainage_markers_visiblity_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("chainage_markers_visiblity_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? ChainageMarkersVisibilityDependent { get; set; }

    [JsonProperty(PropertyName = "static_overlay_services_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("static_overlay_services_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? StaticOverlayServicesDependent { get; set; }

    [JsonProperty(PropertyName = "ui_element_dependent", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("ui_element_dependent")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? UIElementDependent { get; set; }

    [JsonProperty(PropertyName = "ui_element_focus", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("ui_element_focus")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] UIElementFocus { get; set; }

    [JsonProperty(PropertyName = "favorite_priority", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("favorite_priority")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? FavoritePriority { get; set; }

    [JsonProperty(PropertyName = "client_name", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("client_name")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string ClientName { get; set; }

    [JsonProperty(PropertyName = "is_graphics_tool", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("is_graphics_tool")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsGraphicsTool { get; set; }

    [JsonProperty(PropertyName = "sketch_only_editable_if_tool_tab_is_active", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("sketch_only_editable_if_tool_tab_is_active")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? SketchOnlyEditableIfToolTabIsActive { get; set; }


    [JsonProperty(PropertyName = "help_urlpath", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("help_urlpath")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string HelpUrlPath { get; set; }
    [JsonProperty(PropertyName = "help_urlpath_defaulttool", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("help_urlpath_defaulttool")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string HelpUrlPathDefaultTool { get; set; }

    #region IHtml

    public string ToHtmlString()
    {
        return HtmlHelper.ToTable(
            new string[] { "Name", "Id" },
            new object[] { this.name, this.id }
            );
    }

    #endregion
}