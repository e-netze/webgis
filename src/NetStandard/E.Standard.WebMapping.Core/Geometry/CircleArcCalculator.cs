using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Geometry;

// MapMarkup measure tool 'circle_arc': circular arc through three points (start, intermediate point, end)
// same calculation as L.CircleArc.calc (webgis-graphics.circle_arc.js), coordinates in a metric (calc) crs
public class CircleArcCalculator
{
    private CircleArcCalculator(Point center, double radius, double start, double sweep)
    {
        Center = center;
        Radius = radius;
        Start = start;
        Sweep = sweep;
    }

    public Point Center { get; }
    public double Radius { get; }
    public double Start { get; }       // radians
    public double Sweep { get; }       // radians, signed (> 0 => counterclockwise)

    public double Length => Radius * Math.Abs(Sweep);
    public double AngleDegrees => Math.Abs(Sweep) * 180.0 / Math.PI;
    public double MidDirection => Start + Sweep / 2.0;

    public Point At(double radius, double direction)
        => new Point(Center.X + radius * Math.Cos(direction), Center.Y + radius * Math.Sin(direction));

    // same densification as L.ViewAngle.arc
    public List<Point> ArcPoints()
    {
        int steps = Math.Max(8, (int)Math.Ceiling(AngleDegrees / 3.0));
        var points = new List<Point>();
        for (int i = 0; i <= steps; i++)
        {
            points.Add(At(Radius, Start + Sweep * i / steps));
        }
        return points;
    }

    // null if the points are collinear (or identical)
    static public CircleArcCalculator Calc(IList<Point> p)
    {
        if (p is null || p.Count < 3)
        {
            return null;
        }

        double ax = p[1].X - p[0].X, ay = p[1].Y - p[0].Y,
               bx = p[2].X - p[1].X, by = p[2].Y - p[1].Y;
        double lenA = Math.Sqrt(ax * ax + ay * ay), lenB = Math.Sqrt(bx * bx + by * by);
        double cross = ax * by - ay * bx;
        if (lenA == 0 || lenB == 0 || Math.Abs(cross) / (lenA * lenB) < 1e-9)
        {
            return null;
        }

        double d = 2.0 * (p[0].X * (p[1].Y - p[2].Y) + p[1].X * (p[2].Y - p[0].Y) + p[2].X * (p[0].Y - p[1].Y));
        double s0 = p[0].X * p[0].X + p[0].Y * p[0].Y, s1 = p[1].X * p[1].X + p[1].Y * p[1].Y, s2 = p[2].X * p[2].X + p[2].Y * p[2].Y;
        var center = new Point(
            (s0 * (p[1].Y - p[2].Y) + s1 * (p[2].Y - p[0].Y) + s2 * (p[0].Y - p[1].Y)) / d,
            (s0 * (p[2].X - p[1].X) + s1 * (p[0].X - p[2].X) + s2 * (p[1].X - p[0].X)) / d);
        double radius = Math.Sqrt((p[0].X - center.X) * (p[0].X - center.X) + (p[0].Y - center.Y) * (p[0].Y - center.Y));

        double start = Math.Atan2(p[0].Y - center.Y, p[0].X - center.X), end = Math.Atan2(p[2].Y - center.Y, p[2].X - center.X);
        double sweep = end - start;
        if (cross > 0)  // counterclockwise
        {
            while (sweep <= 0) sweep += 2.0 * Math.PI;
        }
        else            // clockwise
        {
            while (sweep >= 0) sweep -= 2.0 * Math.PI;
        }

        return new CircleArcCalculator(center, radius, start, sweep);
    }

    // export: WGS84 [lng, lat] construction points => densified arc (WGS84) or null (collinear)
    // calculated in the calc crs, without a (known) calc crs a local equirectangular approximation is used (like L.ViewAngle.toCalc)
    static public Polyline ArcPolylineWgs84(IEnumerable<Point> lngLats, int calcCrsId)
    {
        var points = lngLats?.Select(p => new Point(p.X, p.Y)).Take(3).ToList();
        if (points is null || points.Count < 3)
        {
            return null;
        }

        var sRef = calcCrsId > 0 ? CoreApiGlobals.SRefStore?.SpatialReferences?.ById(calcCrsId) : null;
        GeometricTransformerPro transformer = null;
        double kx = 111320.0 * Math.Cos(points[0].Y * Math.PI / 180.0), ky = 110540.0;

        try
        {
            if (sRef != null)
            {
                transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, calcCrsId);
                var shapePoints = new Polyline(new Path(points));
                transformer.Transform(shapePoints);
                points = SpatialAlgorithms.ShapePoints(shapePoints, true);
            }
            else
            {
                points = points.Select(p => new Point(p.X * kx, p.Y * ky)).ToList();
            }

            var arc = Calc(points);
            if (arc is null)
            {
                return null;
            }

            var arcPoints = arc.ArcPoints();
            if (transformer is null)
            {
                arcPoints = arcPoints.Select(p => new Point(p.X / kx, p.Y / ky)).ToList();
            }

            var polyline = new Polyline(new Path(arcPoints));
            transformer?.InvTransform(polyline);

            return polyline;
        }
        finally
        {
            transformer?.Dispose();
        }
    }
}
