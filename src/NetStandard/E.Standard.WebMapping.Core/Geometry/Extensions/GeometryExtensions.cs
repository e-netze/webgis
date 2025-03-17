using E.Standard.WebMapping.Core.Geometry.Topology;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Geometry.Extensions;

static public class GeometryExtensions
{
    const double Espsilon = 1e-11;

    /// <summary>
    /// Remove multiple points and zero length path
    /// </summary>
    /// <param name="polyline"></param>
    /// <returns></returns>
    static public Polyline ToValidPolyline(this Polyline polyline)
    {
        if (polyline == null)
        {
            return null;
        }

        var validPolyline = new Polyline();

        for (int p = 0, pathCount = polyline.PathCount; p < pathCount; p++)
        {
            var path = polyline[p];

            if (path == null || path.PointCount < 2)
            {
                continue;
            }

            var validPath = new Path();
            validPath.AddPoint(new Point(path[0]));
            for (int i = 1, pointCount = path.PointCount; i < pointCount; i++)
            {
                var point = path[i];
                var lastValidPoint = validPath[validPath.PointCount - 1];

                if (point != null && point.Distance(lastValidPoint) > Espsilon)
                {
                    validPath.AddPoint(new Point(point));
                }
            }

            if (validPath.PointCount >= 2)
            {
                validPolyline.AddPath(validPath);
            }
        }

        return validPolyline;
    }

    static public Shape LastPoint(this Shape shape)
    {
        if (shape is Point)
        {
            return shape;
        }

        if (shape is MultiPoint && ((MultiPoint)shape).PointCount > 0)
        {
            return ((MultiPoint)shape)[((MultiPoint)shape).PointCount - 1];
        }

        if (shape is Polyline)
        {
            var polyline = (Polyline)shape;

            for (int p = polyline.PathCount - 1; p >= 0; p--)
            {
                var path = polyline[p];
                if (path.PointCount > 0)
                {
                    return new Polyline(new Path(new List<Point>() { path[path.PointCount - 1] }))
                    {
                        SrsId = polyline.SrsId
                    };
                }
            }
        }

        if (shape is Polygon)
        {
            var polygon = (Polygon)shape;

            for (int r = polygon.RingCount - 1; r >= 0; r--)
            {
                var ring = polygon[r];
                if (ring.PointCount > 0)
                {
                    return new Polygon(new Ring(new List<Point>() { ring[ring.PointCount - 1] }));
                }
            }
        }

        return null;
    }

    static public Point First(this PointCollection pointCollection)
    {
        if (pointCollection == null || pointCollection.PointCount == 0)
        {
            return null;
        }

        return pointCollection[0];
    }

    static public Point Last(this PointCollection pointCollection)
    {
        if (pointCollection == null || pointCollection.PointCount == 0)
        {
            return null;
        }

        return pointCollection[pointCollection.PointCount - 1];
    }

    static public IEnumerable<Point> AllPoints(this Shape shape, bool clone = false)
    {
        if (shape == null)
        {
            return new Point[0];
        }

        return SpatialAlgorithms.ShapePoints(shape, clone);
    }

    static public int CountPoints(this Shape shape)
    {
        if (shape is Point)
        {
            return 1;
        }

        if (shape is PointCollection)
        {
            return ((PointCollection)shape).PointCount;
        }

        if (shape is Polyline && ((Polyline)shape).PathCount > 0)
        {
            return ((Polyline)shape).ToPaths().Sum(p => p.PointCount);
        }

        if (shape is Polygon && ((Polygon)shape).RingCount > 0)
        {
            return ((Polygon)shape).ToPaths().Sum(p => p.PointCount);
        }

        return 0;
    }

    static public IEnumerable<Point> AllPathStartAndEndPoints(this Polyline polyline)
    {
        if (polyline == null || polyline.PathCount == 0)
        {
            return new Point[0];
        }

        List<Point> points = new List<Point>();

        for (int p = 0, to = polyline.PathCount; p < to; p++)
        {
            var path = polyline[p];

            points.Add(path.First());
            points.Add(path.Last());
        }

        return points.Where(p => p != null);
    }

    static public Polyline ToPolyline(this Polygon polygon)
    {
        if (polygon == null || polygon.RingCount == 0)
        {
            return null;
        }

        var polyline = new Polyline();

        for (int r = 0, to = polygon.RingCount; r < to; r++)
        {
            var ring = polygon[r];

            var path = new Path(ring);
            path.ClosePath();

            polyline.AddPath(path);
        }

        return polyline;
    }
}
