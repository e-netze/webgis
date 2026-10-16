#nullable enable

using Newtonsoft.Json;

namespace E.Standard.WebMapping.Core.Renderer.UserLabels;

/// <summary>
/// Service-neutral, user-defined labeling of a layer as defined in the viewer ("Beschriftung").
/// No definition for a layer => the layer is labeled as defined in the service (default).
/// Service implementations (e.g. ArcGIS Server dynamicLayers) translate this model
/// into their own labeling definitions.
/// </summary>
public class UserLabeling
{
    public const int CurrentVersion = 1;

    /// <summary>
    /// No labels at all (also switches off the labels defined in the service)
    /// </summary>
    public const string ModeNone = "none";
    /// <summary>
    /// Labels defined by <see cref="Expression"/> and the font properties
    /// </summary>
    public const string ModeCustom = "custom";

    public const string FontStyleRegular = "regular";
    public const string FontStyleBold = "bold";
    public const string FontStyleItalic = "italic";
    public const string FontStyleBoldItalic = "bolditalic";

    public static readonly string[] FontStyles = [FontStyleRegular, FontStyleBold, FontStyleItalic, FontStyleBoldItalic];

    [JsonProperty("version")]
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonProperty("mode")]
    [System.Text.Json.Serialization.JsonPropertyName("mode")]
    public string Mode { get; set; } = ModeCustom;

    /// <summary>
    /// Label text with field placeholders, e.g. "Fläche: [FLAECHE] m²".
    /// Line breaks (\n) are allowed. See <see cref="UserLabelExpression"/>
    /// </summary>
    [JsonProperty("expression", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("expression")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Expression { get; set; }

    [JsonProperty("fontSize", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("fontSize")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public float? FontSize { get; set; }

    /// <summary>
    /// regular, bold, italic, bolditalic (null => regular)
    /// </summary>
    [JsonProperty("fontStyle", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("fontStyle")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? FontStyle { get; set; }

    /// <summary>
    /// #rrggbb (null => black)
    /// </summary>
    [JsonProperty("fontColor", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("fontColor")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? FontColor { get; set; }

    /// <summary>
    /// #rrggbb (null => no halo)
    /// </summary>
    [JsonProperty("haloColor", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("haloColor")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? HaloColor { get; set; }
}
