#nullable enable

using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Renderer.UserRenderers;

/// <summary>
/// Service-neutral, user-defined renderer as defined in the viewer ("Darstellung").
/// Service implementations (e.g. ArcGIS Server dynamicLayers) translate this model
/// into their own renderer definitions.
/// </summary>
public class UserRenderer
{
    public const int CurrentVersion = 1;

    public const string TypeSimple = "simple";
    public const string TypeUniqueValue = "uniqueValue";

    [JsonProperty("version")]
    [System.Text.Json.Serialization.JsonPropertyName("version")]
    public int Version { get; set; } = CurrentVersion;

    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = TypeSimple;

    /// <summary>
    /// Symbol for the simple renderer
    /// </summary>
    [JsonProperty("symbol", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("symbol")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public UserSymbol? Symbol { get; set; }

    /// <summary>
    /// Field for the unique value renderer
    /// </summary>
    [JsonProperty("field", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("field")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Field { get; set; }

    [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<UserRendererItem>? Items { get; set; }

    /// <summary>
    /// Symbol for all other values (unique value renderer). Null => other values are not drawn
    /// </summary>
    [JsonProperty("defaultSymbol", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("defaultSymbol")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public UserSymbol? DefaultSymbol { get; set; }

    [JsonProperty("defaultLabel", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("defaultLabel")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultLabel { get; set; }
}

public class UserRendererItem
{
    [JsonProperty("value")]
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value { get; set; } = "";

    [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("label")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }

    [JsonProperty("symbol")]
    [System.Text.Json.Serialization.JsonPropertyName("symbol")]
    public UserSymbol? Symbol { get; set; }
}
