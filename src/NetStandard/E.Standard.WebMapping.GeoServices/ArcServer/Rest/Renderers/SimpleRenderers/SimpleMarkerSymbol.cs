using Newtonsoft.Json;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers;

class SimpleMarkerSymbol
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

    [JsonProperty("size")]
    [System.Text.Json.Serialization.JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonProperty("angle")]
    [System.Text.Json.Serialization.JsonPropertyName("angle")]
    public int Angle { get; set; }

    [JsonProperty("xoffset")]
    [System.Text.Json.Serialization.JsonPropertyName("xoffset")]
    public int Xoffset { get; set; }

    [JsonProperty("yoffset")]
    [System.Text.Json.Serialization.JsonPropertyName("yoffset")]
    public int Yoffset { get; set; }


    [JsonProperty("outline")]
    [System.Text.Json.Serialization.JsonPropertyName("outline")]
    public Outline Outline { get; set; }
}

/*
Simple Marker Symbol
Simple marker symbols can be used to symbolize point geometries. The type property for simple marker symbols is esriSMS. The angle property defines the number of degrees (0 to 360) that a marker symbol is rotated. The rotation is from East in a counter-clockwise direction where East is the 0° axis.
New in 10.1

Support for esriSMSTriangle was added.

JSON Syntax
{
"type" : "esriSMS",
"style" : "< esriSMSCircle | esriSMSCross | esriSMSDiamond | esriSMSSquare | esriSMSX | esriSMSTriangle >",
"color" : <color>,
"size" : <size>,
"angle" : <angle>,
"xoffset" : <xoffset>,
"yoffset" : <yoffset>,
"outline" : { //if outline has been specified
  "color" : <color>,
  "width" : <width>
}
}

JSON Example
{
"type": "esriSMS",
 "style": "esriSMSSquare",
 "color": [76,115,0,255],
 "size": 8,
 "angle": 0,
 "xoffset": 0,
 "yoffset": 0,
 "outline": 
  {
  "color": [152,230,0,255],
   "width": 1
  }
}    
*/

