using E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Models;

public class GeorefImageMetadata
{
    [JsonProperty(PropertyName = "id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "img_extension")]
    [System.Text.Json.Serialization.JsonPropertyName("img_extension")]
    public string ImageExtension { get; set; }

    [JsonProperty(PropertyName = "width")]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public int ImageWidth { get; set; }

    [JsonProperty(PropertyName = "height")]
    [System.Text.Json.Serialization.JsonPropertyName("height")]
    public int ImageHeight { get; set; }

    [JsonProperty(PropertyName = "topLeft", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("topLeft")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public GeoPosition TopLeft { get; set; }

    [JsonProperty(PropertyName = "topRight", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("topRight")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public GeoPosition TopRight { get; set; }

    [JsonProperty(PropertyName = "bottomLeft", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("bottomLeft")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public GeoPosition BottomLeft { get; set; }

    [JsonProperty(PropertyName = "passPoints", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("passPoints")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<PassPoint> PassPoints { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string ImageFileTitle => $"{this.Id.GeorefImageIdToStorageName()}";


}
