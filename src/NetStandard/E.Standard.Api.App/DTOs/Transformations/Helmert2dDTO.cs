using Newtonsoft.Json;

namespace E.Standard.Api.App.DTOs.Transformations;

public sealed class Helmert2dDTO
{
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "srs")]
    [System.Text.Json.Serialization.JsonPropertyName("srs")]
    public int SrsId { get; set; }

    [JsonProperty(PropertyName = "Cx")]
    [System.Text.Json.Serialization.JsonPropertyName("Cx")]
    public double TransX { get; set; }

    [JsonProperty(PropertyName = "Cy")]
    [System.Text.Json.Serialization.JsonPropertyName("Cy")]
    public double TransY { get; set; }

    [JsonProperty(PropertyName = "Rx")]
    [System.Text.Json.Serialization.JsonPropertyName("Rx")]
    public double Rx { get; set; }

    [JsonProperty(PropertyName = "Ry")]
    [System.Text.Json.Serialization.JsonPropertyName("Ry")]
    public double Ry { get; set; }

    [JsonProperty(PropertyName = "r")]
    [System.Text.Json.Serialization.JsonPropertyName("r")]
    public double Rotation { get; set; }

    [JsonProperty(PropertyName = "scale")]
    [System.Text.Json.Serialization.JsonPropertyName("scale")]
    public double Scale { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public double RLng { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public double RLat { get; set; }
}