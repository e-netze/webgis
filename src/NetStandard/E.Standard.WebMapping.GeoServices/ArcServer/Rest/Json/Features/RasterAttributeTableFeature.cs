using Newtonsoft.Json;
using System.Dynamic;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Features;

class RasterAttributeTableFeature
{
    [JsonProperty("attributes")]
    [System.Text.Json.Serialization.JsonPropertyName("attributes")]
    public ExpandoObject Attributes { get; set; }

    //public class AttributesClass
    //{
    //    [JsonProperty("Value")]
    //    [System.Text.Json.Serialization.JsonPropertyName("Value")]
    //    public object Value { get; set; }

    //    [JsonProperty("ClassName")]
    //    [System.Text.Json.Serialization.JsonPropertyName("ClassName")]
    //    public string ClassName { get; set; }

    //    [JsonProperty("Red")]
    //    [System.Text.Json.Serialization.JsonPropertyName("Red")]
    //    public int Red { get; set; }

    //    [JsonProperty("Green")]
    //    [System.Text.Json.Serialization.JsonPropertyName("Green")]
    //    public int Green { get; set; }

    //    [JsonProperty("Blue")]
    //    [System.Text.Json.Serialization.JsonPropertyName("Blue")]
    //    public int Blue { get; set; }
    //}
}
