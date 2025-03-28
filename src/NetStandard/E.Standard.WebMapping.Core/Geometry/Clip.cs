using E.Standard.WebMapping.Core.Geometry.Clipper;
using System.Threading;

namespace E.Standard.WebMapping.Core.Geometry;

public class Clip
{
    public static Shape PerformClip(Shape clipper, Shape clippee)
    {
        if (clipper == null || clippee == null)
        {
            return null;
        }

        if (clipper is Envelope)
        {
            return ClipEnvelope(new Envelope(clipper.ShapeEnvelope), clippee);
        }
        return null;
    }

    private static Shape ClipEnvelope(Envelope envelope, Shape clippee)
    {
        if (envelope == null || clippee == null)
        {
            return null;
        }

        Envelope geomEnv = clippee.ShapeEnvelope;

        if (!envelope.Intersects(geomEnv))
        {
            return null;
        }

        if (geomEnv.MinX >= envelope.MinX && geomEnv.MaxX <= envelope.MaxX &&
            geomEnv.MinY >= envelope.MinY && geomEnv.MaxY <= envelope.MaxY)
        {
            // Full included...
            return clippee;
        }

        if (clippee is MultiPoint)
        {
            // Point ist schon durch den oberen Test enthalten...
            MultiPoint multipoint = (MultiPoint)clippee;
            MultiPoint newMultiPoint = new MultiPoint();

            for (int i = 0; i < multipoint.PointCount; i++)
            {
                Point point = ClipPoint2Envelope(envelope, multipoint[i]);
                if (point != null)
                {
                    newMultiPoint.AddPoint(point);
                }
            }
            return newMultiPoint;
        }
        if (clippee is Polyline)
        {
            return ClipPolyline2Envelope(envelope, (Polyline)clippee);
        }
        if (clippee is Polygon)
        {
            //GeomPolygon clipperGeom = new GeomPolygon(envelope);
            //GeomPolygon clippeeGeom = new GeomPolygon((Polygon)clippee);

            //GeomPolygon result = clippeeGeom.Clip(ClipOperation.Intersection, clipperGeom);
            //int x = result.NofContours;
            //return result.ToPolygon();
            return ((Polygon)clippee).Clip(envelope, new CancellationTokenSource());
        }
        return null;
    }

    private static Point ClipPoint2Envelope(Envelope envelope, Point point)
    {
        if (point.X >= envelope.MinX && point.X <= envelope.MaxX &&
            point.Y >= envelope.MinY && point.Y <= envelope.MaxY)
        {
            return new Point(point.X, point.Y);
        }
        return null;
    }

    private static Polyline ClipPolyline2Envelope(Envelope envelope, Polyline line)
    {
        Polyline newLine = new Polyline();

        for (int pathIndex = 0; pathIndex < line.PathCount; pathIndex++)
        {
            Path path = line[pathIndex];
            if (path.PointCount < 2)
            {
                continue;
            }

            Point p1 = path[0];
            Path newPath = null;
            for (int i = 1; i < path.PointCount; i++)
            {
                Point p2 = path[i];
                LineClipType type;
                Point[] points = LiamBarsky(envelope, p1, p2, out type);

                switch (type)
                {
                    case LineClipType.inside:
                    case LineClipType.entering:
                        if (newPath == null)
                        {
                            newPath = new Path();
                            newPath.AddPoint(points[0]);
                        }
                        newPath.AddPoint(points[1]);
                        break;
                    case LineClipType.outside:
                        if (newPath != null)
                        {
                            newLine.AddPath(newPath);
                            newPath = null;
                        }
                        break;
                    case LineClipType.leaving:
                        if (newPath == null)
                        {
                            newPath = new Path();
                            newPath.AddPoint(points[0]);
                        }
                        newPath.AddPoint(points[1]);
                        newLine.AddPath(newPath);
                        newPath = null;
                        break;
                }

                p1 = p2;
            }
            if (newPath != null)
            {
                newLine.AddPath(newPath);
            }
        }

        if (newLine.PathCount > 0)
        {
            return newLine;
        }

        return null;
    }
    private enum LineClipType { entering, leaving, outside, inside }
    private static Point[] LiamBarsky(Envelope envelope, Point p1, Point p2, out LineClipType type)
    {
        Point[] points = new Point[2];

        int code1 = CalculateLRBT(envelope, p1);
        int code2 = CalculateLRBT(envelope, p2);

        type = LineClipType.inside;
        if (code1 == 0 && code2 == 0)
        {
            points[0] = p1;
            points[1] = p2;
            return points;
        }
        if ((code1 & code2) != 0)
        {
            type = LineClipType.outside;
            return null;
        }

        double[] pdiff = { p2.X - p1.X, p2.Y - p1.Y };
        double[] p0 = { p1.X, p1.Y };
        double tpe = 1e10; //0.0;
        double tpl = 1e10; //1.0;

        for (int edge = 0; edge < 4; edge++)
        {
            double[] N = new double[2];
            double[] pe = new double[2];
            switch (edge)
            {
                case 0:
                    N[0] = -1.0; N[1] = 0.0;
                    pe[0] = envelope.MinX; pe[1] = 0.0;
                    break;
                case 1:
                    N[0] = 0.0; N[1] = -1.0;
                    pe[0] = 0.0; pe[1] = envelope.MinY;
                    break;
                case 2:
                    N[0] = 1.0; N[1] = 0.0;
                    pe[0] = envelope.MaxX; pe[1] = 0.0;
                    break;
                case 3:
                    N[0] = 0.0; N[1] = 1.0;
                    pe[0] = 0.0; pe[1] = envelope.MaxY;
                    break;
            }

            double t = SolveLineFactorT(N, pe, p0, pdiff, out type);
            if (t >= 0 && t <= 1.0)
            {
                if (type == LineClipType.entering && (t > tpe || tpe == 1e10))
                {
                    tpe = t;
                }
                else if (type == LineClipType.leaving && (t < tpl || tpl == 1e10))
                {
                    tpl = t;
                }
            }
        }

        if (tpe == 1e10 && tpl == 1e10)
        {
            type = LineClipType.outside;
            return null;
        }

        double tpe_ = (tpe != 1e10) ? tpe : 0.0;
        double tpl_ = (tpl != 1e10) ? tpl : 1.0;

        points[0] = new Point(p1.X + tpe_ * pdiff[0], p1.Y + tpe_ * pdiff[1]);
        points[1] = new Point(p1.X + tpl_ * pdiff[0], p1.Y + tpl_ * pdiff[1]);

        code1 = CalculateLRBT(envelope, points[0]);
        code2 = CalculateLRBT(envelope, points[0]);

        if (code1 != 0 || code2 != 0)
        {
            type = LineClipType.outside;
            return null;
        }

        if (tpl_ < 1.0)
        {
            type = LineClipType.leaving;
            return points;
        }
        else if (tpe_ > 0.0)
        {
            type = LineClipType.entering;
            return points;
        }
        return null;
    }

    private static double SolveLineFactorT(double[] N, double[] pe, double[] p0, double[] pdiff, out LineClipType type)
    {
        double dominator = N[0] * pdiff[0] + N[1] * pdiff[1];

        if (dominator < 0.0)
        {
            type = LineClipType.entering;
        }
        else
        {
            type = LineClipType.leaving;
        }

        if (dominator == 0.0)
        {
            return 1e10; // parallel lines...
        }

        return (N[0] * (pe[0] - p0[0]) + N[1] * (pe[1] - p0[1])) / dominator;
    }
    // Code from Cohen-Sutherland 
    private static int CalculateLRBT(Envelope env, Point p)
    {
        //   1001   0001   0101
        //   1000   0000   0100
        //   1010   0010   0110

        int code = 0;
        if (p.X < env.MinX)
        {
            code = code | 0x1000;
        }

        if (p.X > env.MaxX)
        {
            code = code | 0x0100;
        }

        if (p.Y < env.MinY)
        {
            code = code | 0x0010;
        }

        if (p.Y > env.MaxY)
        {
            code = code | 0x0001;
        }

        return code;
    }
}
