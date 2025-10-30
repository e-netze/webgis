#nullable enable

using Newtonsoft.Json;
using System;
using System.Text;
using System.Text.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonLayer
{
    public JsonLayer()
    {
        HasM = HasZ = false;
    }

    [JsonProperty("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonProperty("geometryType")]
    [System.Text.Json.Serialization.JsonPropertyName("geometryType")]
    public string? GeometryType { get; set; }

    [JsonProperty("subLayers")]
    [System.Text.Json.Serialization.JsonPropertyName("subLayers")]
    public JsonLayer[]? SubLayers { get; set; }

    [JsonProperty("parentLayer")]
    [System.Text.Json.Serialization.JsonPropertyName("parentLayer")]
    public JsonLayer? ParentLayer { get; set; }

    [JsonProperty("minScale")]
    [System.Text.Json.Serialization.JsonPropertyName("minScale")]
    public double MinScale { get; set; }

    [JsonProperty("effectiveMinScale")]
    [System.Text.Json.Serialization.JsonPropertyName("effectiveMinScale")]
    public double EffectiveMinScale { get; set; }

    [JsonProperty("maxScale")]
    [System.Text.Json.Serialization.JsonPropertyName("maxScale")]
    public double MaxScale { get; set; }

    [JsonProperty("effectiveMaxScale")]
    [System.Text.Json.Serialization.JsonPropertyName("effectiveMaxScale")]
    public double EffectiveMaxScale { get; set; }

    [JsonProperty("defaultVisibility")]
    [System.Text.Json.Serialization.JsonPropertyName("defaultVisibility")]
    public bool DefaultVisibility { get; set; }

    [JsonProperty("fields")]
    [System.Text.Json.Serialization.JsonPropertyName("fields")]
    public JsonField[]? Fields { get; set; }

    [JsonProperty("extent")]
    [System.Text.Json.Serialization.JsonPropertyName("extent")]
    public JsonExtent? Extent { get; set; }

    [JsonProperty("drawingInfo")]
    [System.Text.Json.Serialization.JsonPropertyName("drawingInfo")]
    public JsonDrawingInfo? DrawingInfo { get; set; }

    [JsonProperty("hasZ")]
    [System.Text.Json.Serialization.JsonPropertyName("hasZ")]
    public bool HasZ { get; set; }

    [JsonProperty("hasM")]
    [System.Text.Json.Serialization.JsonPropertyName("hasM")]
    public bool HasM { get; set; }

    [JsonProperty("description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonProperty("hasAttachments")]
    [System.Text.Json.Serialization.JsonPropertyName("hasAttachments")]
    public bool HasAttachments { get; set; } = false;

    [JsonProperty("timeInfo")]
    [System.Text.Json.Serialization.JsonPropertyName("timeInfo")]
    public JsonTimeInfo? TimeInfo { get; set; }

    [JsonProperty("supportsDynamicLegends")]
    [System.Text.Json.Serialization.JsonPropertyName("supportsDynamicLegends")]
    public bool SupportsDynamicLegends { get; set; } = false;


    public string FullName
    {
        get
        {
            StringBuilder sb = new StringBuilder();

            if (this.ParentLayer != null)
            {
                sb.Append(this.ParentLayer.FullName);
                sb.Append("\\");
            }
            sb.Append(this.Name);

            return sb.ToString();
        }
    }

    public string ParentFullName
    {
        get
        {
            if (this.ParentLayer != null)
            {
                return ParentLayer.FullName + @"\";
            }

            return String.Empty;
        }
    }
}
