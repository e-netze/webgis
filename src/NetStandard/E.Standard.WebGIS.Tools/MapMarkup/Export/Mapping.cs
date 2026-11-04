using System.Collections.Generic;

using E.Standard.Esri.Shapefile;

namespace E.Standard.WebGIS.Tools.MapMarkup.Export;

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
                { "ROTATION", "font-rotation" },  // degrees, clockwise
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
        },
        {
            "view_angle", new Dictionary<string, string>()
            {
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" },
                { "ANGLE", "va-angle" },  // always degrees
                { "UNIT", "va-unit" }
            }
        },
        {
            "intersection_angle", new Dictionary<string, string>()
            {
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" },
                { "UNIT", "ia-unit" },
                { "ANGLES", "ia-angles" }
            }
        },
        {
            "circle_arc", new Dictionary<string, string>()
            {
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" },
                { "UNIT", "ca-unit" },
                { "LABELS", "ca-labels" },
                { "RADIUS", "ca-radius" },
                { "ARC_LEN", "ca-arc-length" },
                { "ANGLE_DEG", "ca-angle-deg" },
                { "ANGLE_GON", "ca-angle-gon" }
            }
        },
        {
            "perpendicular", new Dictionary<string, string>()
            {
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" },
                { "LINE_VERTS", "pp-line-vertices" },
                { "STAT_FMT", "pp-station-format" },
                { "LEN_UNIT", "pp-length-unit" }
            }
        },
        {
            "dimline", new Dictionary<string, string>()
            {
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" },
                { "LEN_UNIT", "length-unit" },
                { "ANGLE_UNIT", "dim-angle-unit" },
                { "ANGLE_TYPE", "dim-angle-type" }
            }
        },
        {
            "dimpolygon", new Dictionary<string, string>()
            {
                { "FILLCOL", "fill" },
                { "BORDERCOL", "stroke" },
                { "TRANSP", "fill-opacity" },
                { "AREA_UNIT", "area-unit" },
                { "ANGLE_UNIT", "dim-angle-unit" }
            }
        },
        {
            "buffer_zone", new Dictionary<string, string>()
            {
                { "FILLCOL", "fill" },
                { "BORDERCOL", "stroke" },
                { "TRANSP", "fill-opacity" },
                { "SRC_TYPE", "bz-source-type" },
                { "BUF_WIDTH", "bz-width" }
            }
        },
        {
            // one polyline per connection line (A => B), the attributes are set per line by the ShapeFileExport
            "object_distance", new Dictionary<string, string>()
            {
                { "NR", "" },
                { "LENGTH_M", "" },
                { "LAYER_A", "" },
                { "OID_A", "" },
                { "LAYER_B", "" },
                { "OID_B", "" },
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" }
            }
        },
        {
            // one polygon per cell, LABEL/COL/ROW are set per cell by the ShapeFileExport
            "measure_grid", new Dictionary<string, string>()
            {
                { "LABEL", "" },
                { "COL", "" },
                { "ROW", "" },
                { "WIDTH", "stroke-width" },
                { "COLOR", "stroke" },
                { "CELL_W", "mg-cell-width" },
                { "CELL_H", "mg-cell-height" },
                { "ALIGN", "mg-align" }
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
        {  "distance_circle", ShapeFile.geometryType.Polygon },
        {  "view_angle", ShapeFile.geometryType.Polyline },
        {  "intersection_angle", ShapeFile.geometryType.Polyline },
        {  "circle_arc", ShapeFile.geometryType.Polyline },
        {  "perpendicular", ShapeFile.geometryType.Polyline },
        {  "dimline", ShapeFile.geometryType.Polyline },
        {  "dimpolygon", ShapeFile.geometryType.Polygon },
        {  "buffer_zone", ShapeFile.geometryType.Polygon },
        {  "object_distance", ShapeFile.geometryType.Polyline },
        {  "measure_grid", ShapeFile.geometryType.Polygon }
    };
}
