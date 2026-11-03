using E.Standard.WebMapping.Core.Geometry.Clipper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace E.Standard.WebMapping.Core.Geometry;

// MapMarkup "add from selection" with the option "merge objects": merges the selected geometries to one shape
// polygons: union (dissolve), lines: paths are connected at their end points, points: multipoint
// input/output coordinates are WGS84, the union is calculated in the calc crs (meters, clipper precision)
// without a (known) calc crs a local equirectangular approximation is used (like BufferZoneCalculator)
static public class ShapeMerger
{
    public const double LineEndTolerance = 0.01;  // meters

    // null: no shapes or mixed geometry types
    static public Shape Merge(IEnumerable<Shape> shapes, int calcCrsId)
    {
        var list = shapes?.Where(s => s != null).ToList();
        if (list is null || list.Count == 0)
        {
            return null;
        }

        bool allPolygons = list.All(s => s is Polygon),
             allLines = list.All(s => s is Polyline),
             allPoints = list.All(s => s is Point || s is MultiPoint);

        if (allPoints)
        {
            return new MultiPoint(list.SelectMany(s => SpatialAlgorithms.ShapePoints(s, true)).Select(p => new Point(p.X, p.Y)));
        }
        if (!allPolygons && !allLines)
        {
            return null;
        }

        // work on copies => the input shapes stay unchanged (WGS84)
        list = list.Select(s => s is Polygon polygon
                        ? (Shape)new Polygon(Enumerable.Range(0, polygon.RingCount).Select(r => new Ring(CopyPoints(polygon[r]))))
                        : new Polyline(Enumerable.Range(0, ((Polyline)s).PathCount).Select(p => new Path(CopyPoints(((Polyline)s)[p])))))
                   .ToList();

        var firstPoint = list.SelectMany(s => SpatialAlgorithms.ShapePoints(s, false)).FirstOrDefault();
        if (firstPoint is null)
        {
            return null;
        }

        var sRef = calcCrsId > 0 ? CoreApiGlobals.SRefStore?.SpatialReferences?.ById(calcCrsId) : null;
        GeometricTransformerPro transformer = null;
        double kx = 111320.0 * Math.Cos(firstPoint.Y * Math.PI / 180.0), ky = 110540.0;

        try
        {
            if (sRef != null)
            {
                transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, calcCrsId);
            }

            foreach (var shape in list)
            {
                if (transformer != null)
                {
                    transformer.Transform(shape);
                }
                else
                {
                    Scale(shape, kx, ky);
                }
            }

            Shape result;
            using (var cts = new CancellationTokenSource())
            {
                result = allPolygons
                    ? list.Cast<Polygon>().ToList().Merge(cts)
                    : JoinLines(list.Cast<Polyline>(), LineEndTolerance);
            }

            if (result != null)
            {
                if (transformer != null)
                {
                    transformer.InvTransform(result);
                }
                else
                {
                    Scale(result, 1.0 / kx, 1.0 / ky);
                }
            }

            return result;
        }
        finally
        {
            transformer?.Dispose();
        }
    }

    static private List<Point> CopyPoints(PointCollection points)
        => points.ToArray().Select(p => new Point(p.X, p.Y)).ToList();

    static private void Scale(Shape shape, double kx, double ky)
    {
        foreach (var point in SpatialAlgorithms.ShapePoints(shape, false))
        {
            point.X *= kx;
            point.Y *= ky;
        }
    }

    // greedy: a path is extended as long as another path starts or ends at one of its end points
    // paths that can't be connected remain separate parts (=> multipart)
    static private Polyline JoinLines(IEnumerable<Polyline> polylines, double tolerance)
    {
        var paths = polylines
            .SelectMany(polyline => Enumerable.Range(0, polyline.PathCount).Select(i => polyline[i]))
            .Where(path => path.PointCount > 0)
            .Select(path => path.ToArray().Select(p => new Point(p.X, p.Y)).ToList())
            .ToList();

        var result = new List<List<Point>>();

        while (paths.Count > 0)
        {
            var current = paths[0];
            paths.RemoveAt(0);

            bool joined = true;
            while (joined)
            {
                joined = false;
                for (int i = 0; i < paths.Count; i++)
                {
                    var candidate = paths[i];
                    Point first = current[0], last = current[current.Count - 1];
                    Point cFirst = candidate[0], cLast = candidate[candidate.Count - 1];

                    if (Dist(last, cFirst) <= tolerance)
                    {
                        current.AddRange(candidate.Skip(1));
                    }
                    else if (Dist(last, cLast) <= tolerance)
                    {
                        current.AddRange(Enumerable.Reverse(candidate).Skip(1));
                    }
                    else if (Dist(first, cLast) <= tolerance)
                    {
                        current.InsertRange(0, candidate.Take(candidate.Count - 1));
                    }
                    else if (Dist(first, cFirst) <= tolerance)
                    {
                        current.InsertRange(0, Enumerable.Reverse(candidate).Take(candidate.Count - 1));
                    }
                    else
                    {
                        continue;
                    }

                    paths.RemoveAt(i);
                    joined = true;
                    break;
                }
            }

            result.Add(current);
        }

        return new Polyline(result.Select(points => new Path(points)));
    }

    static private double Dist(Point a, Point b)
        => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
