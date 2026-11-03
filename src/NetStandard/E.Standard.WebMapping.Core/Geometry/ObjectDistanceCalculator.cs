using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry;

// MapMarkup measure tool 'object_distance' (Objektabstand): shortest connection between two features
// (point, multipoint, line, polygon, also multipart). Polygons are treated as their boundary (all rings incl. holes):
// a point/line inside a polygon gets the distance to the boundary.
// input shapes are WGS84 [lng, lat], the distance is calculated in the calc crs (meters)
// without a (known) calc crs a local equirectangular approximation is used (like BufferZoneCalculator)
static public class ObjectDistanceCalculator
{
    // meters: shorter connections => the objects touch/intersect (no dimension line)
    public const double TouchTolerance = 1e-4;

    public class Result
    {
        public double[] From { get; set; }  // on A, WGS84 [lng, lat]
        public double[] To { get; set; }    // on B, WGS84 [lng, lat]
        public double Length { get; set; }  // meters (calc crs)
        public bool Touching { get; set; }
    }

    static public Result Calc(Shape a, Shape b, int calcCrsId)
    {
        var compA = new Components();
        var compB = new Components();
        compA.Collect(a);
        compB.Collect(b);

        if (compA.IsEmpty || compB.IsEmpty)
        {
            return null;
        }

        var sRef = calcCrsId > 0 ? CoreApiGlobals.SRefStore?.SpatialReferences?.ById(calcCrsId) : null;
        GeometricTransformerPro transformer = null;
        double kx = 111320.0 * Math.Cos(compA.Coords[0].Y * Math.PI / 180.0), ky = 110540.0;

        try
        {
            if (sRef != null)
            {
                transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, calcCrsId);
            }

            compA.Project(transformer, kx, ky);
            compB.Project(transformer, kx, ky);

            var best = Closest(compA, compB);
            if (best is null)
            {
                return null;
            }

            double[] x = [best.Value.from.X, best.Value.to.X], y = [best.Value.from.Y, best.Value.to.Y];
            if (transformer != null)
            {
                var line = new Polyline(new Path(new List<Point>() { new Point(x[0], y[0]), new Point(x[1], y[1]) }));
                transformer.InvTransform(line);
                x = [line[0][0].X, line[0][1].X];
                y = [line[0][0].Y, line[0][1].Y];
            }
            else
            {
                x = [x[0] / kx, x[1] / kx];
                y = [y[0] / ky, y[1] / ky];
            }

            return new Result()
            {
                From = [Math.Round(x[0], 9), Math.Round(y[0], 9)],
                To = [Math.Round(x[1], 9), Math.Round(y[1], 9)],
                Length = best.Value.dist,
                Touching = best.Value.dist <= TouchTolerance
            };
        }
        finally
        {
            transformer?.Dispose();
        }
    }

    #region Distance

    static private (Point from, Point to, double dist)? Closest(Components a, Components b)
    {
        double best = double.MaxValue;
        Point bestFrom = null, bestTo = null;

        void Check(Point from, Point to)
        {
            double d = Dist(from, to);
            if (d < best)
            {
                best = d;
                bestFrom = from;
                bestTo = to;
            }
        }

        foreach (var pa in a.Points)
        {
            foreach (var pb in b.Points)
            {
                Check(pa, pb);
            }
            foreach (var sb in b.Segments)
            {
                if (sb.BoxDistance(pa) < best)
                {
                    Check(pa, ClosestOnSegment(pa, sb.P1, sb.P2));
                }
            }
        }

        foreach (var sa in a.Segments)
        {
            foreach (var pb in b.Points)
            {
                if (sa.BoxDistance(pb) < best)
                {
                    Check(ClosestOnSegment(pb, sa.P1, sa.P2), pb);
                }
            }
            foreach (var sb in b.Segments)
            {
                if (sa.BoxDistance(sb) >= best)
                {
                    continue;
                }

                if (Intersect(sa.P1, sa.P2, sb.P1, sb.P2, out Point ip))
                {
                    Check(ip, ip);
                    continue;
                }

                // no intersection => the shortest connection starts at one of the 4 end points
                Check(sa.P1, ClosestOnSegment(sa.P1, sb.P1, sb.P2));
                Check(sa.P2, ClosestOnSegment(sa.P2, sb.P1, sb.P2));
                Check(ClosestOnSegment(sb.P1, sa.P1, sa.P2), sb.P1);
                Check(ClosestOnSegment(sb.P2, sa.P1, sa.P2), sb.P2);
            }
        }

        return bestFrom is null ? null : (bestFrom, bestTo, best);
    }

    static private double Dist(Point p1, Point p2)
        => Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));

    static private Point ClosestOnSegment(Point p, Point s1, Point s2)
    {
        double dx = s2.X - s1.X, dy = s2.Y - s1.Y, len2 = dx * dx + dy * dy;
        if (len2 == 0)
        {
            return s1;
        }

        double t = Math.Max(0, Math.Min(1, ((p.X - s1.X) * dx + (p.Y - s1.Y) * dy) / len2));
        return new Point(s1.X + t * dx, s1.Y + t * dy);
    }

    // parallel/collinear segments => false (an overlap is found by the end point distances)
    static private bool Intersect(Point p1, Point p2, Point q1, Point q2, out Point ip)
    {
        ip = null;
        double rx = p2.X - p1.X, ry = p2.Y - p1.Y, sx = q2.X - q1.X, sy = q2.Y - q1.Y;
        double denom = rx * sy - ry * sx;
        if (Math.Abs(denom) < 1e-12)
        {
            return false;
        }

        double qpx = q1.X - p1.X, qpy = q1.Y - p1.Y;
        double t = (qpx * sy - qpy * sx) / denom, u = (qpx * ry - qpy * rx) / denom;
        if (t < 0 || t > 1 || u < 0 || u > 1)
        {
            return false;
        }

        ip = new Point(p1.X + t * rx, p1.Y + t * ry);
        return true;
    }

    #endregion

    #region Components

    private class Segment
    {
        public Segment(Point p1, Point p2) { P1 = p1; P2 = p2; }

        public Point P1 { get; }
        public Point P2 { get; }

        private double MinX => Math.Min(P1.X, P2.X);
        private double MaxX => Math.Max(P1.X, P2.X);
        private double MinY => Math.Min(P1.Y, P2.Y);
        private double MaxY => Math.Max(P1.Y, P2.Y);

        // lower bound of the distance (bounding boxes) => skip segments that can't be closer
        public double BoxDistance(Point p)
            => Math.Sqrt(Sqr(Math.Max(0, Math.Max(MinX - p.X, p.X - MaxX))) + Sqr(Math.Max(0, Math.Max(MinY - p.Y, p.Y - MaxY))));

        public double BoxDistance(Segment s)
            => Math.Sqrt(Sqr(Math.Max(0, Math.Max(MinX - s.MaxX, s.MinX - MaxX))) + Sqr(Math.Max(0, Math.Max(MinY - s.MaxY, s.MinY - MaxY))));

        static private double Sqr(double v) => v * v;
    }

    // single points and segments of a shape, the segments reference the points in Coords (projected in place)
    private class Components
    {
        public List<Point> Coords { get; } = new();
        public List<Point> Points { get; } = new();
        public List<Segment> Segments { get; } = new();

        public bool IsEmpty => Points.Count == 0 && Segments.Count == 0;

        public void Collect(Shape shape)
        {
            switch (shape)
            {
                case null:
                    break;
                case AggregateShape aggregate:
                    for (int i = 0; i < aggregate.CountShapes; i++)
                    {
                        Collect(aggregate[i]);
                    }
                    break;
                case Point point:
                    Points.Add(Add(point));
                    break;
                case Polyline polyline:
                    for (int i = 0; i < polyline.PathCount; i++)
                    {
                        AddPath(polyline[i], false);
                    }
                    break;
                case Polygon polygon:
                    for (int i = 0; i < polygon.RingCount; i++)
                    {
                        AddPath(polygon[i], true);
                    }
                    break;
                case PointCollection points:  // MultiPoint
                    for (int i = 0; i < points.PointCount; i++)
                    {
                        Points.Add(Add(points[i]));
                    }
                    break;
            }
        }

        public void Project(GeometricTransformerPro transformer, double kx, double ky)
        {
            if (transformer != null)
            {
                double[] x = new double[Coords.Count], y = new double[Coords.Count];
                for (int i = 0; i < Coords.Count; i++)
                {
                    x[i] = Coords[i].X;
                    y[i] = Coords[i].Y;
                }

                transformer.Transform(x, y);

                for (int i = 0; i < Coords.Count; i++)
                {
                    Coords[i].X = x[i];
                    Coords[i].Y = y[i];
                }
            }
            else
            {
                foreach (var c in Coords)
                {
                    c.X *= kx;
                    c.Y *= ky;
                }
            }
        }

        private Point Add(Point p)
        {
            var copy = new Point(p.X, p.Y);
            Coords.Add(copy);
            return copy;
        }

        private void AddPath(PointCollection path, bool closed)
        {
            if (path is null || path.PointCount == 0)
            {
                return;
            }

            var points = new List<Point>();
            for (int i = 0; i < path.PointCount; i++)
            {
                points.Add(Add(path[i]));
            }

            if (points.Count == 1)
            {
                Points.Add(points[0]);
                return;
            }

            for (int i = 1; i < points.Count; i++)
            {
                Segments.Add(new Segment(points[i - 1], points[i]));
            }

            if (closed && points.Count > 2 && (points[0].X != points[^1].X || points[0].Y != points[^1].Y))
            {
                Segments.Add(new Segment(points[^1], points[0]));
            }
        }
    }

    #endregion
}
