using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers;

class SimpleLineSymbol
{
    [JsonProperty("type")]
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonProperty("style")]
    [System.Text.Json.Serialization.JsonPropertyName("style")]
    public string Style { get; set; }

    [JsonProperty("color")]
    [System.Text.Json.Serialization.JsonPropertyName("color")]
    public int[] Color { get; set; }

    [JsonProperty("width")]
    [System.Text.Json.Serialization.JsonPropertyName("width")]
    public float Width { get; set; }
}

/*
Simple Line Symbol
Simple line symbols can be used to symbolize polyline geometries or outlines for polygon fills. The type property for simple line symbols is esriSLS.

 * JSON Syntax
{
"type" : "esriSLS",
"style" : "< esriSLSDash | esriSLSDashDot | esriSLSDashDotDot | esriSLSDot | esriSLSNull | esriSLSSolid >",
"color" : <color>,
"width" : <width>
}

JSON Example
{
"type": "esriSLS",
"style": "esriSLSDot",
"color": [115,76,0,255],
"width": 1
}
*/
