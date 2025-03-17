using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry.Clipper;
using E.Standard.WebMapping.Core.Geometry.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace E.Standard.WebMapping.Core.Geometry;

public sealed class SpatialAlgorithms
{
    #region Jordan
    public static bool Jordan(Polygon polygon, double x, double y)
    {
        if (polygon == null)
        {
            return false;
        }

        int inter = 0;
        try
        {
            for (int i = 0; i < polygon.RingCount; i++)
            {
                inter += CalcIntersections(polygon[i], x, y);
            }
        }
        catch { return false; }

        return ((inter % 2) == 0) ? false : true;
    }

    public static bool Jordan(Ring ring, Ring hole)
    {
        if (hole == null || hole.PointCount < 3 || ring == null || ring.PointCount < 3)
        {
            return false;
        }

        Polygon polygon = new Polygon(ring);

        for (int i = 0; i < hole.PointCount; i++)
        {
            if (!Jordan(polygon, hole[i].X, hole[i].Y))
            {
                return false;
            }
        }
        return true;
    }

    private static int CalcIntersections(Ring ring, double x, double y)
    {
        bool first = true;
        double x1 = 0.0, y1 = 0.0, x2 = 0, y2 = 0, x0 = 0, y0 = 0, k, d;
        int inter = 0;

        for (int i = 0; i < ring.PointCount; i++)
        {
            Point point = ring[i];
            x2 = point.X - x;
            y2 = point.Y - y;
            if (!first)
            {
                if (isPositive(x1) != isPositive(x2))
                {
                    if (getLineKD(x1, y1, x2, y2, out k, out d))
                    {
                        if (d > 0)
                        {
                            inter++;
                        }
                    }
                }
            }
            x1 = x2;
            y1 = y2;
            if (first)
            {
                first = false;
                x0 = x1; y0 = y1;
            }
        }

        //Ring schliessen
        if (Math.Abs(x0 - x2) > Shape.Epsilon || Math.Abs(y0 - y2) > Shape.Epsilon)
        {
            if (isPositive(x0) != isPositive(x2))
            {
                if (getLineKD(x0, y0, x2, y2, out k, out d))
                {
                    if (d > 0)
                    {
                        inter++;
                    }
                }
            }
        }
        return inter;
    }

    #endregion

    #region generell
    private static bool getLineKD(double x1, double y1, double x2, double y2, out double k, out double d)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        if (Math.Abs(dx) < Shape.Epsilon)
        {
            d = k = 0.0;
            return false;
        }
        k = dy / dx;
        d = y1 - k * x1;  // y=kx+d
        return true;
    }
    private static bool isPositive(double z)
    {
        if (Math.Sign(z) < 0)
        {
            return false;
        }

        return true;
    }

    public static List<Point> ShapePoints(Shape shape, bool clone)
    {
        return ShapePoints(shape, clone, false);
    }
    public static List<Point> ShapePoints(Shape shape, bool clone, bool markStartFigurePoints)
    {
        List<Point> points = new List<Point>();

        if (shape is Point)
        {
            points.Add((clone ? new Point((Point)shape) : (Point)shape));
        }
        else if (shape is PointCollection)
        {
            PointCollection mp = (PointCollection)shape;
            for (int i = 0; i < mp.PointCount; i++)
            {
                points.Add((clone ? new Point(mp[i]) : mp[i]));
            }
        }
        else if (shape is Polyline)
        {
            Polyline pl = (Polyline)shape;
            for (int i = 0; i < pl.PathCount; i++)
            {
                if (pl[i] == null)
                {
                    continue;
                }

                for (int j = 0; j < pl[i].PointCount; j++)
                {
                    if (markStartFigurePoints == true && j == 0)
                    {
                        points.Add(clone ? new PointStartFigure(new Point(pl[i][j])) : new PointStartFigure(pl[i][j]));
                    }
                    else
                    {
                        points.Add((clone ? new Point(pl[i][j]) : pl[i][j]));
                    }
                }
            }
        }
        else if (shape is Polygon)
        {
            Polygon pg = (Polygon)shape;
            for (int i = 0; i < pg.RingCount; i++)
            {
                if (pg[i] == null)
                {
                    continue;
                }

                for (int j = 0; j < pg[i].PointCount; j++)
                {
                    if (markStartFigurePoints == true && j == 0)
                    {
                        points.Add(clone ? new PointStartFigure(new Point(pg[i][j])) : new PointStartFigure(pg[i][j]));
                    }
                    else
                    {
                        points.Add((clone ? new Point(pg[i][j]) : pg[i][j]));
                    }
                }
            }
        }

        return points;
    }

    public static bool IsEqual2D(List<Point> points1, List<Point> points2, double tolerance = Shape.Epsilon)
    {
        if (points1 == null || points2 == null)
        {
            return false;
        }

        if (points1.Count() != points2.Count())
        {
            return false;
        }

        for (int i = 0, to = points1.Count(); i < to; i++)
        {
            Point p1 = points1[i], p2 = points2[i];
            if (p1 == null || p2 == null)
            {
                return false;
            }

            if (p1.Distance2D(p2) > tolerance)
            {
                return false;
            }
        }

        return true;
    }

    public static Point IntersectLine(Point p11, Point p12, Point p21, Point p22, bool between, double tolerance = 1e-7)
    {
        if (p11 == null || p12 == null || p21 == null || p22 == null)
        {
            return null;
        }

        double lx = p21.X - p11.X;
        double ly = p21.Y - p11.Y;

        double r1x = p12.X - p11.X, r1y = p12.Y - p11.Y;
        double r2x = p22.X - p21.X, r2y = p22.Y - p21.Y;

        LinearEquation22 lineq = new LinearEquation22(
            lx, ly,
            r1x, -r2x,
            r1y, -r2y);
        if (lineq.Solve())
        {
            double t1 = lineq.Var1;
            double t2 = lineq.Var2;

            if (between &&
                (t1 < -tolerance || t1 > 1.0 + tolerance ||
                 t2 < -tolerance || t2 > 1.0 + tolerance))
            {
                return null;
            }

            return new Point(p11.X + t1 * r1x, p11.Y + t1 * r1y);
        }

        return null;
    }

    public static void SwapVertices(PointCollection pColl)
    {
        if (pColl == null)
        {
            return;
        }

        List<Point> points = ShapePoints(pColl, false);
        pColl.Clear();

        for (int i = points.Count - 1; i >= 0; i--)
        {
            pColl.AddPoint(points[i]);
        }
    }

    static public void SwapPolylineVertices(Polyline polyline)
    {
        if (polyline == null)
        {
            return;
        }

        for (int p = 0; p < polyline.PathCount; p++)
        {
            Path path = polyline[p];
            SwapVertices(path);
        }
    }

    static public Point[] DeterminePointsOnShape(IMap map, Shape shape, int max = 0, bool isGeographic = false, Point referencePoint = null)
    {
        List<Point> points = new List<Point>();

        if (shape is Point)
        {
            points.Add((Point)shape);
        }
        else if (shape is MultiPoint && ((MultiPoint)shape).PointCount > 0)
        {
            for (int i = 0; i < ((MultiPoint)shape).PointCount; i++)
            {
                points.Add(((MultiPoint)shape)[i]);
            }
        }
        else if (shape is Polyline)
        {
            double length = ((Polyline)shape).Length;
            for (double stat = length / 10.0; stat <= length; stat += length / 10.0)
            {
                Point p = PolylinePoint((Polyline)shape, stat);
                if (p == null)
                {
                    continue;
                }

                if (map != null && map.Extent != null && !map.Extent.Contains(p.ShapeEnvelope))
                {
                    continue;
                }

                points.Add(p);
            }
        }
        else if (shape is Polygon)
        {
            Polygon poly = (Polygon)shape;
            Envelope polyEnv = poly.ShapeEnvelope;

            if (map != null && !map.Extent.Contains(polyEnv))
            {
                if (!map.Extent.Intersects(polyEnv))
                {
                    return points.ToArray();
                }

                poly = Clip.PerformClip(map.Extent, poly) as Polygon;
                if (poly == null || poly.ShapeEnvelope == null || poly.RingCount == 0)
                {
                    return points.ToArray();
                }

                polyEnv = poly.ShapeEnvelope;
            }

            if (poly != null)
            {
                if (referencePoint != null)
                {
                    if (SpatialAlgorithms.Jordan(poly, referencePoint.X, referencePoint.Y))
                    {
                        points.Add(referencePoint);
                    }
                }

                Point cp = poly?.ShapeEnvelope?.CenterPoint;
                if (cp != null)
                {
                    if (SpatialAlgorithms.Jordan(poly, cp.X, cp.Y))
                    {
                        points.Add(cp);
                        //if (max > 0 && points.Count >= max)
                        //    return points.ToArray();
                    }
                }

                #region Spiralen Methode
                /*
                double rStep = 1, tenMeters = isGeographic == false ? 10.0 : UnitConverter.FromMeters(10.0, MapUnits.decimal_degrees);
                if (map != null)
                {
                    rStep = 10.0 * map.MapScale / (map.Dpi / 0.0254);
                }
                else
                {
                    rStep = Math.Max(Math.Min(polyEnv.Width, polyEnv.Height), tenMeters) / 10.0;
                }

                for (double r = rStep; r < Math.Max(polyEnv.Width, polyEnv.Height) / 2.0; r += rStep)
                {
                    double step = rStep / r / 100.0;
                    for (double w = 0; w < 2.0 * Math.PI; w += step)
                    {
                        Point p = new Point(cp.X + r * Math.Cos(w), cp.Y + r * Math.Sin(w));
                        if (Algorithms.Jordan(poly, p.X, p.Y))
                        {
                            points.Add(p);
                            if (max > 0 && points.Count >= max)
                                return points.ToArray();
                        }
                    }
                }
                 * */
                #endregion

                #region Stochastische Methode

                if (points.Count == 0 && polyEnv != null)
                {
                    Random random = new Random();
                    for (int t = 0; t < 1000000; t++)
                    {
                        double t1 = random.NextDouble(), t2 = random.NextDouble();
                        Point p = new Point(polyEnv.MinX + polyEnv.Width * t1, polyEnv.MinY + polyEnv.Height * t2);
                        if (SpatialAlgorithms.Jordan(poly, p.X, p.Y))
                        {
                            points.Add(p);
                            if ((max > 0 && points.Count >= max) || points.Count > 20)
                            {
                                break; //return points.ToArray();
                            }
                        }
                    }
                }

                #endregion
            }
        }
        else if (shape is AggregateShape)
        {
            var aggShape = (AggregateShape)shape;

            for (int a = 0; a < aggShape.CountShapes; a++)
            {
                var aggPart = aggShape[a];

                points.AddRange(DeterminePointsOnShape(map, aggPart, max, isGeographic, referencePoint));
            }
        }
        else if (shape is Envelope)
        {
            points.Add(((Envelope)shape).CenterPoint);
        }

        if (referencePoint != null)
        {
            var hotspot = ClosestPointToHotspot(points.ToArray(), referencePoint);
            if (hotspot != null)
            {
                points = new List<Point>() { hotspot };
            }
        }

        if (points.Count == 0)
        {
            // only for debugging => should never happen ;)
            // throw new Exception("Can't determine a point on shape"); 
        }

        return points.ToArray();
    }

    static public Point ClosestPointToHotspot(Point[] candidates, Point hotspot)
    {
        if (candidates == null)
        {
            return null;
        }

        Point ret = null;
        double dist = double.MaxValue;
        foreach (Point candidate in candidates)
        {
            double d = candidate.Distance(hotspot);
            if (d < dist)
            {
                dist = d;
                ret = candidate;
            }
        }

        return ret;
    }

    static public void Rotate(Shape shape, Point center, double angele360)
    {
        var points = ShapePoints(shape, false, false);

        double angle = angele360 * Math.PI / 180.0;
        double sin_a = Math.Sin(angle), cos_a = Math.Cos(angle);

        foreach (var point in points)
        {
            if (point == null)
            {
                continue;
            }

            point.X = point.X - center.X;
            point.Y = point.Y - center.Y;

            var x = cos_a * point.X + sin_a * point.Y;
            var y = -sin_a * point.X + cos_a * point.Y;

            point.X = x + center.X;
            point.Y = y + center.Y;
        }
    }

    static public void SetSpatialReferenceAndProjectPoints(Shape shape, int srsId, SpatialReferenceCollection sRefCollection)
    {
        if (shape == null)
        {
            return;
        }

        shape.SrsId = srsId;
        if (srsId <= 0)
        {
            return;
        }

        var points = ShapePoints(shape, false);

        if (points != null)
        {
            foreach (var point in points)
            {
                if (point.SrsId > 0 && point.SrsId != shape.SrsId)
                {
                    using (var transformer = new GeometricTransformerPro(sRefCollection, point.SrsId, shape.SrsId))
                    {
                        transformer.Transform(point);
                    }
                }
            }
        }
    }

    #endregion

    #region Buffer

    public static Polygon FastMergePolygon(List<Polygon> polygons, CancellationTokenSource cts)
    {
        cts.ThrowExceptionIfCanceled();

        if (polygons == null || polygons.Count == 0)
        {
            return null;
        }
        if (polygons.Count == 1)
        {
            return polygons[0];
        }

        int count = polygons.Count;
        List<Polygon> merged = new List<Polygon>();

        for (int i = 0; i < polygons.Count; i += 2)
        {
            if (i + 1 < count)
            {
                List<Polygon> p = new List<Polygon>();
                p.Add(polygons[i]);
                p.Add(polygons[i + 1]);

                var mergedPolygons = p.Merge(cts);
                if (mergedPolygons != null)
                {
                    merged.Add(mergedPolygons);
                }
            }
            else
            {
                merged.Add(polygons[i]);
            }
        }

        return FastMergePolygon(merged, cts);
    }

    internal static Polygon PolylineBuffer(Polyline polyline, double distance, CancellationTokenSource cts)
    {
        List<Polygon> buffers = new List<Polygon>();

        polyline = polyline.ToValidPolyline();

        for (int i = 0; i < polyline.PathCount; i++)
        {
            cts.ThrowExceptionIfCanceled();

            PathBuffers(polyline[i], distance, ref buffers);
        }

        return FastMergePolygon(buffers, cts);
    }


    private static void PathBuffers(Path path, double distance, ref List<Polygon> polygons)
    {
        if (polygons == null)
        {
            return;
        }

        List<Point> points = new List<Point>();
        int to = path.PointCount;
        if (to == 0)
        {
            return;
        }

        double minExtent = Math.Max(path.ShapeEnvelope.Width, path.ShapeEnvelope.Height);
        //maxS = Math.Max(maxS, minExtent / 1e5);

        if (path is Ring)
        {
            if (path[0].X != path[to - 1].X || path[0].Y != path[to - 1].Y)
            {
                to += 1;
            }
        }
        for (int i = 0; i < to - 1; i++)
        {
            Point p1 = ((i < path.PointCount) ? path[i] : path[0]);
            Point p2 = ((i + 1 < path.PointCount) ? path[i + 1] : path[0]);
            Point p3 = ((i + 2 < path.PointCount) ? path[i + 2] : path[0]);
            if (i == to - 2)
            {
                p3 = null;
            }

            Vector2D v1 = new Vector2D(p2.X - p1.X, p2.Y - p1.Y);
            Vector2D v2 = (p3 != null) ? new Vector2D(p3.X - p2.X, p3.Y - p2.Y) : null;

            Vector2D pv1 = new Vector2D(v1);
            pv1.Rotate(Math.PI / 2.0);
            pv1.Length = distance;
            Vector2D pv2 = null;
            if (p3 != null)
            {
                pv2 = new Vector2D(v2);
                pv2.Rotate(Math.PI / 2.0);
                pv2.Length = distance;
            }

            Ring ring = new Ring();
            if (i == 0)
            {
                AppendCurve(ring, new Vector2D(p1.X, p1.Y), pv1, pv1 * -1.0, distance);
            }
            else
            {
                ring.AddPoint(new Point(pv1.X + p1.X, pv1.Y + p1.Y));
                ring.AddPoint(new Point(-pv1.X + p1.X, -pv1.Y + p1.Y));
            }

            if (p3 != null)
            {
                v2.Rotate(-v1.Angle);
                if (v2.Angle < Math.PI)
                {
                    AppendCurve(ring, new Vector2D(p2.X, p2.Y), pv1 * -1.0, pv2 * -1.0, distance);
                    //ring.AddPoint(new Point(p2.X, p2.Y));
                    ring.AddPoint(new Point(pv1.X + p2.X, pv1.Y + p2.Y));
                }
                else
                {
                    ring.AddPoint(new Point(-pv1.X + p2.X, -pv1.Y + p2.Y));
                    //ring.AddPoint(new Point(p2.X, p2.Y));
                    AppendCurve(ring, new Vector2D(p2.X, p2.Y), pv2, pv1, distance);
                }
            }
            else
            {
                AppendCurve(ring, new Vector2D(p2.X, p2.Y), pv1 * -1.0, pv1, distance);
            }

            polygons.Add(new Polygon(ring));
        }
    }

    internal static Polygon PointBuffer(Point point, double distance)
    {
        Vector2D v1 = new Vector2D(0, distance);
        Vector2D v2 = new Vector2D(0, distance);
        v2.Rotate(-0.0001);

        Ring ring = new Ring();
        AppendCurve(ring, new Vector2D(point.X, point.Y), v1, v2, distance);

        return new Polygon(ring);
    }
    private static void AppendCurve(PointCollection pColl, Vector2D m, Vector2D v1, Vector2D v2, double distance, double maxS = 0.01)
    {
        double from = v1.Angle;
        double to = v2.Angle;
        if (to < from)
        {
            to += 2.0 * Math.PI;
        }


        //
        //  Scheitel des Kreissegments a = 2*acos(1-x/distance)
        //  wobei x die Scheitelhöhe ist
        //

        distance = Math.Abs(distance);
        double step = distance == 0D ? 2.0 * Math.PI : Math.Min(Math.PI / 5, 2 * Math.Acos(1 - maxS / distance));

        //double step = Math.Min(Math.PI / 5, 1.0 / distance);  // 1m toleranz!!!

        for (double a = from; a < to; a += step)
        {
            pColl.AddPoint(new Point(m.X + distance * Math.Cos(a), m.Y + distance * Math.Sin(a)));
        }
        pColl.AddPoint(new Point(m.X + distance * Math.Cos(to), m.Y + distance * Math.Sin(to)));
    }

    #endregion

    #region Distance

    public static Point Point2PolylineDistance(Polyline polyline, Point point, out double dist, out double stat, double tolerance = 1e-7)
    {
        int sign;
        return Point2PolylineDistance(polyline, point, out dist, out stat, out sign, tolerance);
    }

    public static Point Point2PolylineDistance(Polyline polyline, Point point, out double dist, out double stat, out int sign, double tolerance = 1e-7)
    {
        dist = double.MaxValue;
        double Station = 0.0;
        stat = double.MaxValue;
        double X = 0.0, Y = 0.0;
        double x = point.X, y = point.Y;
        sign = 1;

        if (polyline == null)
        {
            return null;
        }

        try
        {
            for (int p = 0; p < polyline.PathCount; p++)
            {
                Path path = polyline[p];
                if (path == null || path.PointCount == 0)
                {
                    continue;
                }

                double x1, y1, x2, y2, X_, Y_;
                x1 = path[0].X;
                y1 = path[0].Y;
                for (int i = 1; i < path.PointCount; i++)
                {
                    x2 = path[i].X;
                    y2 = path[i].Y;
                    int si;
                    double d = Point2LineDistance2(x1, y1, x2, y2, x, y, out X_, out Y_, out si, tolerance);
                    if (d < dist)
                    {
                        dist = d;
                        X = X_;
                        Y = Y_;
                        stat = Station + Math.Sqrt((X - x1) * (X - x1) + (Y - y1) * (Y - y1));
                        sign = si;
                    }
                    Station += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    x1 = x2; y1 = y2;
                }
            }
        }
        catch
        {
            return null;
        }

        return (X != 0.0 || Y != 0.0) ? new Point(X, Y) : null;
    }

    private static double Point2LineDistance2(double x1, double y1, double x2, double y2, double x, double y,
                                              out double X, out double Y, out int sign,
                                              double tolerance = 1e-7)
    {
        x -= x1;
        y -= y1;
        double dx = x2 - x1, dy = y2 - y1, x_, y_;
        double a = Math.Atan2(dy, dx), len = Math.Sqrt(dx * dx + dy * dy);
        double c = Math.Cos(a), s = Math.Sin(a);

        x_ = c * x + s * y;
        y_ = -s * x + c * y;

        double dist = 1e10;
        if (x_ < -tolerance)
        {
            dist = Math.Sqrt(x_ * x_ + y_ * y_);
            X = 0; Y = 0;
        }
        else if (x_ > len + tolerance)
        {
            dist = Math.Sqrt((x_ - len) * (x_ - len) + y_ * y_);
            X = 0; Y = 0;
        }
        else
        {
            dist = Math.Abs(y_);
            X = x1 + x_ * c;
            Y = y1 + x_ * s;
        }
        sign = y_ < 0 ? -1 : 1;
        return dist;
    }

    public static double Point2InfiniteLineDistance(double x1, double y1, double x2, double y2, double x, double y, out double X, out double Y, out int sign)
    {
        x -= x1;
        y -= y1;
        double dx = x2 - x1, dy = y2 - y1, x_, y_;
        double a = Math.Atan2(dy, dx), len = Math.Sqrt(dx * dx + dy * dy);
        double c = Math.Cos(a), s = Math.Sin(a);

        x_ = c * x + s * y;
        y_ = -s * x + c * y;

        double dist = Math.Abs(y_);
        X = x1 + x_ * c;
        Y = y1 + x_ * s;
        sign = y_ < 0 ? -1 : 1;

        return dist;
    }

    public static double Point2ShapeDistance(Shape shape, Point point)
    {
        int sign;
        return Point2ShapeDistance(shape, point, out sign);
    }

    public static double Point2ShapeDistance(Shape shape, Point point, out int sign)
    {
        sign = 1;
        if (shape == null || point == null)
        {
            return double.MaxValue;
        }

        if (shape is Point)
        {
            return ((Point)shape).Distance2D(point);
        }
        else if (shape is Polyline)
        {
            double dist, stat;
            Point2PolylineDistance((Polyline)shape, point, out dist, out stat, out sign);
            return dist;
        }
        else if (shape is Polygon)
        {
            var polygon = (Polygon)shape;
            if (Jordan(polygon, point.X, point.Y))
            {
                return 0D;
            }

            double dist = double.MaxValue;
            foreach (var ring in polygon.Rings)
            {
                ring.ClosePath();

                dist = Math.Min(Point2ShapeDistance(ring.ToPolyline(), point, out sign), dist);
            }
            return dist;
        }
        else if (shape is PointCollection)
        {
            double ret = double.MaxValue;
            for (int i = 0; i < ((PointCollection)shape).PointCount; i++)
            {
                Point p = ((PointCollection)shape)[i];
                double dist = p.Distance2D(point);
                if (dist < ret)
                {
                    ret = dist;
                }
            }
            return ret;
        }

        return double.MaxValue;
    }

    public static void SnapToPolyline(IEnumerable<Shape> shapes, Polyline snapper, double tolerance)
    {
        if (shapes == null)
        {
            return;
        }

        foreach (var shape in shapes)
        {
            var shapePoints = ShapePoints(shape, false);
            if (shapePoints == null)
            {
                continue;
            }

            foreach (var shapePoint in shapePoints)
            {
                double dist, stat;

                var snappedPoint = Point2PolylineDistance(snapper, shapePoint, out dist, out stat, tolerance);
                if (snappedPoint != null && dist <= 1.1 * tolerance)
                {
                    shapePoint.X = snappedPoint.X;
                    shapePoint.Y = snappedPoint.Y;
                }
            }
        }
    }

    #endregion

    #region Stat

    public static Polyline PolylineSplit(Polyline polyline, double from, double to)
    {
        if (polyline == null)
        {
            return null;
        }

        Polyline polylinePart = new Polyline();
        bool firstPointFound = false, lastPointFound = false;

        double stat = 0.0;
        for (int p = 0; p < polyline.PathCount; p++)
        {
            Path path = polyline[p];
            if (path == null || path.PointCount == 0)
            {
                continue;
            }

            PointCollection pColl = new PointCollection();

            double x1, y1, x2, y2;
            x1 = path[0].X;
            y1 = path[0].Y;
            for (int i = 1; i < path.PointCount; i++)
            {
                x2 = path[i].X;
                y2 = path[i].Y;

                stat += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                if (!firstPointFound && stat >= from && stat >= to)  // from und to liegen aufen einem Segment
                {
                    if (!firstPointFound)
                    {
                        pColl.AddPoint(PolylinePoint(polyline, from));
                        firstPointFound = true;
                    }
                    if (!lastPointFound)
                    {
                        pColl.AddPoint(PolylinePoint(polyline, to));
                        lastPointFound = true;
                        break;
                    }
                }
                else if (stat >= from && stat <= to)
                {
                    if (!firstPointFound)
                    {
                        pColl.AddPoint(PolylinePoint(polyline, from));
                        firstPointFound = true;
                    }
                    pColl.AddPoint(new Point(x2, y2));
                }
                else if (stat > to)
                {
                    if (!lastPointFound)
                    {
                        pColl.AddPoint(PolylinePoint(polyline, to));
                        lastPointFound = true;
                        break;
                    }
                }
                x1 = x2; y1 = y2;
            }
            /*
            if (Station >= from && Station <= to)
            {
                if (!firstPointFound)
                {
                    pColl.AddPoint(PolylinePoint(polyline, from));
                    firstPointFound = true;
                }
                pColl.AddPoint(new Point(x1, y1));
            }
            else if (Station > to)
            {
                if (!lastPointFound)
                {
                    pColl.AddPoint(PolylinePoint(polyline, to));
                    lastPointFound = true;
                    break;
                }
            }
            else*/
            if (stat <= to && !lastPointFound)
            {
                pColl.AddPoint(new Point(x1, y1));
            }

            //if (Station >= from && Station <= to && lastPointFound == false)
            //    pColl.AddPoint(new Point(x1, y1));

            if (pColl.PointCount > 0)
            {
                polylinePart.AddPath(new Path(RemoveDoubles(pColl)));
            }

            if (lastPointFound == true)
            {
                break;
            }
        }

        return polylinePart.PathCount > 0 ? polylinePart : null;
    }

    public static Point PolylinePoint(Polyline polyline, double stat)
    {
        double direction;
        return PolylinePoint(polyline, stat, out direction);
    }

    public static Point PolylinePoint(Polyline polyline, double stat, out double direction)
    {
        direction = double.NaN;

        if (polyline == null)
        {
            return null;
        }

        double station = 0.0, station0 = 0.0;
        for (int p = 0; p < polyline.PathCount; p++)
        {
            Path path = polyline[p];
            if (path == null || path.PointCount == 0)
            {
                continue;
            }

            Path newPath = new Path();

            double x1, y1, x2, y2;
            x1 = path[0].X;
            y1 = path[0].Y;
            for (int i = 1; i < path.PointCount; i++)
            {
                x2 = path[i].X;
                y2 = path[i].Y;

                station0 = station;
                station += Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                if (station >= stat)
                {
                    double t = stat - station0;
                    double l = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    double dx = (x2 - x1) / l, dy = (y2 - y1) / l;

                    direction = Math.Atan2(dy, dx);

                    return new Point(x1 + dx * t, y1 + dy * t);
                }

                x1 = x2; y1 = y2;
            }
        }

        return null;
    }

    private static PointCollection RemoveDoubles(PointCollection pColl)
    {
        PointCollection newColl = new PointCollection();
        if (pColl.PointCount == 0)
        {
            return newColl;
        }

        newColl.AddPoint(new Point(pColl[0]));
        for (int i = 1; i < pColl.PointCount; i++)
        {
            if (newColl[newColl.PointCount - 1].Equals(pColl[i]) == false)
            {
                newColl.AddPoint(pColl[i]);
            }
        }

        return newColl;
    }
    #endregion

    #region HelperClasses

    // Diese Point Art markiert in einer Liste von Punkte dein Start eines Pfades oder Ringes.
    // wird von Algorithms.ShapePoints verwendet und zurückgegeben
    public class PointStartFigure : PointM
    {
        readonly Point _p;
        public PointStartFigure(Point p)
            : base(p.X, p.Y, null)
        {
            _p = p;
            if (p is PointM)
            {
                base.M = ((PointM)p).M;
            }
        }

        new public object M
        {
            get
            {
                if (_p is PointM)
                {
                    return ((PointM)_p).M;
                }

                return null;
            }
            set
            {
                if (_p is PointM)
                {
                    ((PointM)_p).M = value;
                }
            }
        }

        new public double X
        {
            get { return _p.X; }
            set { _p.X = value; }
        }
        new public double Y
        {
            get { return _p.Y; }
            set { _p.Y = value; }
        }
        new public double Z
        {
            get { return _p.Z; }
            set { _p.Z = value; }
        }
    }

    #endregion
}
