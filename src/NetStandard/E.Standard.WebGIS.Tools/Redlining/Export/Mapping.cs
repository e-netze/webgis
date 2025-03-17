using E.Standard.Esri.Shapefile;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Redlining.Export;

static class Mapping
{
    static public Dictionary<string, Dictionary<string, string>> ShapePropertyMapping = new Dictionary<string, Dictionary<string, string>>()
    {
        {
            "symbol" , new Dictionary<string, string>()
            {
                { "SYMBOL", "symbol" },
                { "TEXT", "_meta.text" }
            }
        },
        {
            "text", new Dictionary<string, string>()
            {
                { "SIZE","font-size" },
                { "COLOR", "font-color" },
                { "TEXT", "_meta.text" }
            }
        },
        {
            "point", new Dictionary<string, string>()
            {
                { "SIZE","point-size" },
                { "COLOR", "point-color" }
            }
        },
        {
            "line", new Dictionary<string, string>()
            {
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" },
                { "TEXT", "_meta.text" }
            }
        },
        {
            "freehand", new Dictionary<string, string>()
            {
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" },
                { "TEXT", "_meta.text" }
            }
        },
        {
            "polygon", new Dictionary<string, string>()
            {
                { "FILLCOL", "fill" },
                { "BORDERCOL", "stroke" },
                { "TRANSP", "fill-opacity" },
                { "TEXT", "_meta.text" }
            }
        },
        {
            "rectangle", new Dictionary<string, string>()
            {
                { "FILLCOL", "fill" },
                { "BORDERCOL", "stroke" },
                { "TRANSP", "fill-opacity" },
                { "TEXT", "_meta.text" }
            }
        },
        {
            "circle", new Dictionary<string, string>()
            {
                { "FILLCOL", "fill" },
                { "BORDERCOL", "stroke" },
                { "TRANSP", "fill-opacity" },
                { "RADIUS", "circle-radius" },
                { "TEXT", "_meta.text" }
            }
        },
        {
            "distance_circle", new Dictionary<string, string>()
            {
                { "FILLCOL", "fill" },
                { "BORDERCOL", "stroke" },
                { "TRANSP", "fill-opacity" },
                { "RADIUS", "dc-radius" },
                { "STEPS", "dc-steps" },
                { "TEXT", "_meta.text" }
            }
        }
    };

    static public Dictionary<string, ShapeFile.geometryType> ShapeGeometryMapping = new Dictionary<string, ShapeFile.geometryType>()
    {
        {  "symbol", ShapeFile.geometryType.Point },
        {  "text", ShapeFile.geometryType.Point },
        {  "point", ShapeFile.geometryType.Point },
        {  "line", ShapeFile.geometryType.Polyline },
        {  "freehand", ShapeFile.geometryType.Polyline },
        {  "polygon", ShapeFile.geometryType.Polygon },
        {  "rectangle", ShapeFile.geometryType.Polygon },
        {  "circle", ShapeFile.geometryType.Polygon },
        {  "distance_circle", ShapeFile.geometryType.Polygon }
    };
}
