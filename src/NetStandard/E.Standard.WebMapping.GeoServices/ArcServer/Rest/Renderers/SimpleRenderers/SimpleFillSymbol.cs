using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers;

class SimpleFillSymbol
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

    [JsonProperty("outline")]
    [System.Text.Json.Serialization.JsonPropertyName("outline")]
    public Outline Outline { get; set; }
}

/*
Simple Fill Symbol
Simple fill symbols can be used to symbolize polygon geometries. The type property for simple fill symbols is esriSFS.

JSON Syntax
{
"type" : "esriSFS",
"style" : "< esriSFSBackwardDiagonal | esriSFSCross | esriSFSDiagonalCross | esriSFSForwardDiagonal | esriSFSHorizontal | esriSFSNull | esriSFSSolid | esriSFSVertical >",
"color" : <color>,
"outline" : <simpleLineSymbol> //if outline has been specified
}

JSON Example
{
  "type": "esriSFS",
  "style": "esriSFSSolid",
  "color": [115,76,0,255],
    "outline": {
     "type": "esriSLS",
     "style": "esriSLSSolid",
     "color": [110,110,110,255],
     "width": 1
	     }
}
*/
