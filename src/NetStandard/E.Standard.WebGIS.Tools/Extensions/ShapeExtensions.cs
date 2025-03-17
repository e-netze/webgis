using E.Standard.WebMapping.Core.Geometry;
using System;

namespace E.Standard.WebGIS.Tools.Extensions;

static class ShapeExtensions
{
    public static string GetToolName(this Shape shape)
    {
        if (shape is Point)
        {
            return "symbol";
        }

        if (shape is Polyline)
        {
            return "line";
        }

        if (shape is Polygon)
        {
            return "polygon";
        }

        return String.Empty;
    }

    public static string GetGeometryName(this Shape shape)
    {
        var typeName = shape?.GetType().ToString();
        if (!String.IsNullOrEmpty(typeName) && typeName.Contains("."))
        {
            typeName = typeName.Substring(typeName.LastIndexOf(".") + 1);
        }

        return typeName;
    }
}
