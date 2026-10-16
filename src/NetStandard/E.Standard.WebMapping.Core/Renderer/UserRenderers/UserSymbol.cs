#nullable enable

using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Renderer.UserRenderers;

/// <summary>
/// Service-neutral symbol. Which properties are used depends on the geometry type of the layer:
/// point:   Marker, Size, Color, Opacity, OutlineColor, OutlineWidth
/// line:    Color, Width, LineStyle
/// polygon: Color, Opacity, FillStyle, OutlineColor, OutlineWidth, OutlineStyle
/// Sizes and widths are in pt. Colors are hex strings (#rrggbb).
/// </summary>
public class UserSymbol
{
    public static readonly string[] Markers = ["circle", "square", "diamond", "triangle", "cross", "x"];
    public static readonly string[] LineStyles = ["solid", "dash", "dot", "dashdot"];
    public static readonly string[] FillStyles = ["solid", "none", "horizontal", "vertical", "cross", "forwarddiagonal", "backwarddiagonal", "diagonalcross"];

    [JsonProperty("color", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("color")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }

    /// <summary>
    /// Fill opacity (0..1) for points and polygons
    /// </summary>
    [JsonProperty("opacity", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("opacity")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public float? Opacity { get; set; }

    [JsonProperty("marker", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("marker")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Marker { get; set; }

    [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("size")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public float? Size { get; set; }

    [JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public float? Width { get; set; }

    [JsonProperty("lineStyle", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("lineStyle")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? LineStyle { get; set; }

    [JsonProperty("fillStyle", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("fillStyle")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? FillStyle { get; set; }

    [JsonProperty("outlineColor", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("outlineColor")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? OutlineColor { get; set; }

    [JsonProperty("outlineWidth", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("outlineWidth")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public float? OutlineWidth { get; set; }

    [JsonProperty("outlineStyle", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("outlineStyle")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? OutlineStyle { get; set; }
}
