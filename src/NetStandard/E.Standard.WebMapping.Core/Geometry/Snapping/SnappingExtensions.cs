using E.Standard.WebMapping.Core.Geometry.Extensions;
using System;

namespace E.Standard.WebMapping.Core.Geometry.Snapping;

static public class SnappingExtensions
{
    static public SnappingTypes ToSnappingTypes(this string[] types)
    {
        SnappingTypes result = SnappingTypes.None;

        foreach (var type in types)
        {
            if (Enum.TryParse<SnappingTypes>(type, true, out SnappingTypes snappingType))
            {
                result |= snappingType;
            }
        }

        return result;
    }

    static public void SnapTo(this Shape shape, Shape snapper, SnappingTypes snappingTypes, double tolerance = 1e-8)
    {
        var shapePoints = shape.AllPoints(false);

        foreach (var shapePoint in shapePoints)
        {
            if (shapePoint.IsSnapped == true)
            {
                continue;
            }

            if (snapper is Point && ((Point)snapper).TrySnapPoint(shapePoint, snappingTypes, tolerance))
            {
                shapePoint.IsSnapped = true;
            }
            else if (snapper is Polyline && ((Polyline)snapper).TrySnapPoint(shapePoint, snappingTypes, tolerance))
            {
                shapePoint.IsSnapped = true;
            }
            else if (snapper is Polygon && ((Polygon)snapper).TrySnapPoint(shapePoint, snappingTypes, tolerance))
            {
                shapePoint.IsSnapped = true;
            }
        }
    }

    static public bool TrySnapPoint(this Point snapperPoint, Point point, SnappingTypes snappingTypes, double tolerance = 1e-8)
    {
        if (!snappingTypes.HasFlag(SnappingTypes.Nodes) &&
            !snappingTypes.HasFlag(SnappingTypes.Endpoints))
        {
            return false;
        }

        return snapperPoint.Distance2D(point) <= tolerance;
    }

    static public bool TrySnapPoint(this Polyline snapperPolyline, Point point, SnappingTypes snappingTypes, double tolerance = 1e-8)
    {
        if (snappingTypes.HasFlag(SnappingTypes.Endpoints))
        {
            foreach (var endPoint in snapperPolyline.AllPathStartAndEndPoints())
            {
                if (endPoint.Distance2D(point) <= tolerance)
                {
                    return true;
                }
            }
        }

        if (snappingTypes.HasFlag(SnappingTypes.Nodes))
        {
            foreach (var node in snapperPolyline.AllPoints())
            {
                if (node.Distance2D(point) <= tolerance)
                {
                    return true;
                }
            }
        }

        if (snappingTypes.HasFlag(SnappingTypes.Edges))
        {
            double dist, stat;
            if (SpatialAlgorithms.Point2PolylineDistance(snapperPolyline, point, out dist, out stat) != null)
            {
                if (dist <= tolerance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    static public bool TrySnapPoint(this Polygon snapperPolygon, Point point, SnappingTypes snappingTypes, double tolerance = 1e-8)
    {
        if (snappingTypes.HasFlag(SnappingTypes.Nodes))
        {
            foreach (var node in snapperPolygon.AllPoints())
            {
                if (node.Distance2D(point) <= tolerance)
                {
                    return true;
                }
            }
        }

        if (snappingTypes.HasFlag(SnappingTypes.Edges))
        {
            var snapperPolyline = snapperPolygon.ToPolyline();

            double dist, stat;
            if (SpatialAlgorithms.Point2PolylineDistance(snapperPolyline, point, out dist, out stat) != null)
            {
                if (dist <= tolerance)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
