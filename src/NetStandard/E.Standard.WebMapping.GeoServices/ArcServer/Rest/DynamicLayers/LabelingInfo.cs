using E.Standard.WebMapping.Core;
using Newtonsoft.Json;
using System;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.DynamicLayers;

public class LabelingInfo
{
    [JsonProperty("labelPlacement", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("labelPlacement")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string LabelPlacement { get; set; }

    [JsonProperty("labelExpression", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("labelExpression")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string LabelExpression { get; set; }

    [JsonProperty("useCodedValues", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("useCodedValues")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? UseCodedValues { get; set; }

    [JsonProperty("symbol", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("symbol")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public object Symbol { get; set; }

    [JsonProperty("minScale", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("minScale")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? MinScale { get; set; }

    [JsonProperty("maxScale", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("maxScale")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxScale { get; set; }

    [JsonProperty("where", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("where")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string Where { get; set; }

    public static string DefaultLabelPlacement(LayerType type)
    {
        switch (type)
        {
            case LayerType.point:
                return "esriServerPointLabelPlacementAboveRight";
            case LayerType.line:
                return "esriServerLinePlacementAboveAlong";
            case LayerType.polygon:
                return "esriServerPolygonPlacementAlwaysHorizontal";
        }

        return String.Empty;
    }
}

/*
* 
Label Placement Values For Point Features
esriServerPointLabelPlacementAboveCenter 	esriServerPointLabelPlacementAboveLeft 	esriServerPointLabelPlacementAboveRight
esriServerPointLabelPlacementBelowCenter 	esriServerPointLabelPlacementBelowLeft 	esriServerPointLabelPlacementBelowRight
esriServerPointLabelPlacementCenterCenter 	esriServerPointLabelPlacementCenterLeft 	esriServerPointLabelPlacementCenterRight
* 
Label Placement Values For Line Features
esriServerLinePlacementAboveAfter 	esriServerLinePlacementAboveAlong 	esriServerLinePlacementAboveBefore
esriServerLinePlacementAboveStart 	esriServerLinePlacementAboveEnd 	 
esriServerLinePlacementBelowAfter 	esriServerLinePlacementBelowAlong 	esriServerLinePlacementBelowBefore
esriServerLinePlacementBelowStart 	esriServerLinePlacementBelowEnd 	 
esriServerLinePlacementCenterAfter 	esriServerLinePlacementCenterAlong 	esriServerLinePlacementCenterBefore
esriServerLinePlacementCenterStart 	esriServerLinePlacementCenterEnd 	 
* 
Label Placement Values For Polygon Features
esriServerPolygonPlacementAlwaysHorizontal

* 
* 
*  Example
{
"labelPlacement": "esriServerPointLabelPlacementAboveRight",
"labelExpression": "[NAME]",
"useCodedValues": false,
"symbol": {
 "type": "esriTS",
 "color": [38,115,0,255],
 "backgroundColor": null,
 "borderLineColor": null,
 "verticalAlignment": "bottom",
 "horizontalAlignment": "left",
 "rightToLeft": false,
 "angle": 0,
 "xoffset": 0,
 "yoffset": 0,
 "kerning": true,
 "font": {
  "family": "Arial",
  "size": 11,
  "style": "normal",
  "weight": "bold",
  "decoration": "none"
 }
},
"minScale": 0,
"maxScale": 0,
"where" : "NAME LIKE 'A%'" //label only those feature where name begins with A
} 
* 
*/


