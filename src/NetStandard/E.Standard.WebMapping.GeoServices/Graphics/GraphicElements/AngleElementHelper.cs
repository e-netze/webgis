using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// shared drawing logic for the MapMarkup angle measure tools (ViewAngleElement, IntersectionAngleElement, CircleArcElement, ...)
// the geometry must be the same as in the client: webgis-graphics.view_angle.js, webgis-graphics.intersection_angle.js, webgis-graphics.circle_arc.js, ...
internal class AngleElementHelper : IDisposable
{
    private readonly IMap _map;
    private readonly ICanvas _canvas;
    private readonly GeometricTransformerPro _calcTransformer;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly float _fontSize;
    private readonly float _dpiFactor;

    public AngleElementHelper(ICanvas canvas, IMap map, Shape shape, Shape calcShape, ArgbColor color, float width, float fontSize)
    {
        _canvas = canvas;
        _map = map;
        _color = color;
        _width = width;
        _fontSize = fontSize > 0 ? fontSize : 11f;
        _dpiFactor = map.DpiFactor();

        CalcPoints = SpatialAlgorithms.ShapePoints(calcShape ?? shape, true);
        _calcTransformer = calcShape != null
            ? new GeometricTransformerPro(CoreApiGlobals.SRefStore, shape.SrsId, calcShape.SrsId)
            : null;
    }

    public List<Point> CalcPoints { get; }

    public Point ToImage(double x, double y)
    {
        var point = new Point(x, y);
        _calcTransformer?.InvTransform(point);
        return _map.WorldToImage(point);
    }

    public void DrawLine(IEnumerable<Point> calcPoints, bool dashed, float? width = null)
    {
        var imagePoints = calcPoints.Select(p => ToImage(p.X, p.Y)).ToArray();
        if (imagePoints.Length < 2)
        {
            return;
        }

        using (var pen = Current.Engine.CreatePen(_color, (width ?? _width) * _dpiFactor))
        using (IGraphicsPath gp = Current.Engine.CreateGraphicsPath())
        {
            pen.DashStyle = dashed ? LineDashStyle.Dash : LineDashStyle.Solid;
            pen.StartCap = pen.EndCap = LineCap.Round;

            gp.StartFigure();
            for (int i = 1; i < imagePoints.Length; i++)
            {
                gp.AddLine((float)imagePoints[i - 1].X, (float)imagePoints[i - 1].Y, (float)imagePoints[i].X, (float)imagePoints[i].Y);
            }
            _canvas.DrawPath(pen, gp);
        }
    }

    public void DrawArc(Point center, double radius, double from, double to)
    {
        int steps = Math.Max(8, (int)Math.Ceiling(Math.Abs(to - from) * 180.0 / Math.PI / 3.0));
        var points = new List<Point>();
        for (int i = 0; i <= steps; i++)
        {
            points.Add(At(center, radius, from + (to - from) * i / steps));
        }

        DrawLine(points, false);
    }

    public void DrawLabel(Point calcPoint, string text, bool alignNear = false)
    {
        var imagePoint = ToImage(calcPoint.X, calcPoint.Y);

        using (var font = Current.Engine.CreateFont(E.Standard.Platform.SystemInfo.DefaultFontName, _fontSize * _dpiFactor))
        using (var brush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
        using (var haloBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
        {
            var format = Current.Engine.CreateDrawTextFormat();
            format.Alignment = alignNear ? StringAlignment.Near : StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;

            _canvas.DrawOutlineLabel(_map, text, imagePoint, font, brush, haloBrush, format);
        }
    }

    // segment length label parallel to the segment (above the line), like dimline (PolylineElement)
    public void DrawSegmentLength(Point calcFrom, Point calcTo, double length, Unit unit)
    {
        Point from = ToImage(calcFrom.X, calcFrom.Y), to = ToImage(calcTo.X, calcTo.Y);

        _canvas.LabelSegmentLength(_map,
                                   (float)from.X, (float)from.Y, (float)to.X, (float)to.Y,
                                   length,
                                   fontSize: _fontSize,
                                   dpiFactor: _dpiFactor,
                                   lineAlignment: StringAlignment.Far,
                                   unit: unit);
    }

    // label parallel to the direction calcDirFrom => calcDirTo (always readable), at calcAt
    // above => text above the direction (like dimline), otherwise below
    public void DrawParallelLabel(Point calcAt, Point calcDirFrom, Point calcDirTo, string text, bool above)
    {
        Point at = ToImage(calcAt.X, calcAt.Y);

        try
        {
            using (var font = Current.Engine.CreateFont(E.Standard.Platform.SystemInfo.DefaultFontName, _fontSize * _dpiFactor))
            using (var brush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
            using (var haloBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
            {
                _canvas.TranslateTransform(new CanvasPointF((float)at.X, (float)at.Y));
                _canvas.RotateTransform(ImageTextAngle(calcDirFrom, calcDirTo));

                var format = Current.Engine.CreateDrawTextFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = above ? StringAlignment.Far : StringAlignment.Near;

                _canvas.DrawOutlineLabel(_map, text, new Point(0, 0), font, brush, haloBrush, format);
            }
        }
        catch { }
        finally
        {
            _canvas.ResetTransform();
        }
    }

    // is calcPoint above the text written at calcAt in direction calcDirFrom => calcDirTo (see DrawParallelLabel)
    public bool IsAboveText(Point calcAt, Point calcDirFrom, Point calcDirTo, Point calcPoint)
    {
        Point at = ToImage(calcAt.X, calcAt.Y), point = ToImage(calcPoint.X, calcPoint.Y);
        double angle = ImageTextAngle(calcDirFrom, calcDirTo) * Math.PI / 180.0;
        double px = point.X - at.X, py = point.Y - at.Y;

        return (-Math.Sin(angle) * px + Math.Cos(angle) * py) < 0;  // image y axis points down
    }

    public void FillPoint(Point calcPoint, float size)
    {
        var point = ToImage(calcPoint.X, calcPoint.Y);
        float r = size * _dpiFactor;

        using (var brush = Current.Engine.CreateSolidBrush(_color))
        {
            _canvas.FillEllipse(brush, (float)point.X - r, (float)point.Y - r, r * 2f, r * 2f);
        }
    }

    // image angle in degrees of the direction, always readable (not upside down)
    private float ImageTextAngle(Point calcFrom, Point calcTo)
    {
        Point from = ToImage(calcFrom.X, calcFrom.Y), to = ToImage(calcTo.X, calcTo.Y);

        var angle = (float)(Math.Atan2(to.Y - from.Y, to.X - from.X) * 180.0 / Math.PI);
        if (angle < 0)
        {
            angle += 360f;
        }
        if (angle > 90f && angle < 270f)
        {
            angle += 180f;
        }
        return angle;
    }

    static public Point At(Point center, double radius, double direction)
        => new Point(center.X + radius * Math.Cos(direction), center.Y + radius * Math.Sin(direction));

    static public string FormatNumber(double value)
        => Math.Round(value, 2).ToString("0.00", CultureInfo.CurrentCulture);

    static public string FormatAngle(double degrees, string unit)
    {
        string deg = $"{FormatNumber(degrees)}°", gon = $"{FormatNumber(degrees * 10.0 / 9.0)} gon";

        return unit?.ToLowerInvariant() switch
        {
            "gon" => gon,
            "both" => $"{deg} / {gon}",
            _ => deg
        };
    }

    #region Vertex angles (dimline, dimpolygon)

    public record VertexAngle(Point Center, double Radius, double Start, double Delta, double Degrees, string Side, bool Extension);

    // same geometry as L.ViewAngle.vertexAngles (webgis-graphics.view_angle.js)
    //   closed (polygon) => interior angle 0-360° (orientation of the ring), otherwise only at the inner vertices:
    //   angleType "inner" => angle between the segments 0-180°, "deflection" => change of direction 0-180° with side L/R
    static public List<VertexAngle> VertexAngles(IList<Point> points, bool closed, string angleType)
    {
        var pts = new List<Point>(points);
        var result = new List<VertexAngle>();

        if (closed && pts.Count > 1 && pts[0].X == pts[^1].X && pts[0].Y == pts[^1].Y)
        {
            pts.RemoveAt(pts.Count - 1);
        }

        int n = pts.Count;
        if (n < 3)
        {
            return result;
        }

        double signedArea = 0;
        if (closed)
        {
            for (int k = 0; k < n; k++)
            {
                Point p1 = pts[k], p2 = pts[(k + 1) % n];
                signedArea += p1.X * p2.Y - p2.X * p1.Y;
            }
            if (signedArea == 0)
            {
                return result;
            }
        }

        static double Normalize(double a)  // => (-PI, PI]
        {
            while (a > Math.PI) a -= 2.0 * Math.PI;
            while (a <= -Math.PI) a += 2.0 * Math.PI;
            return a;
        }

        for (int i = closed ? 0 : 1; i < (closed ? n : n - 1); i++)
        {
            Point c = pts[i], prev = pts[(i - 1 + n) % n], next = pts[(i + 1) % n];
            double lenPrev = Math.Sqrt((prev.X - c.X) * (prev.X - c.X) + (prev.Y - c.Y) * (prev.Y - c.Y)),
                   lenNext = Math.Sqrt((next.X - c.X) * (next.X - c.X) + (next.Y - c.Y) * (next.Y - c.Y));
            if (lenPrev == 0 || lenNext == 0)
            {
                continue;
            }

            double aPrev = Math.Atan2(prev.Y - c.Y, prev.X - c.X), aNext = Math.Atan2(next.Y - c.Y, next.X - c.X);
            double radius = 0.2 * Math.Min(lenPrev, lenNext), start, delta;
            string side = String.Empty;
            bool extension = false;

            if (closed)
            {
                // counter clockwise ring => interior from the direction to the next vertex counter clockwise to the direction to the previous vertex
                double from = signedArea > 0 ? aNext : aPrev, to = signedArea > 0 ? aPrev : aNext;
                double interior = to - from;
                while (interior < 0) interior += 2.0 * Math.PI;
                while (interior >= 2.0 * Math.PI) interior -= 2.0 * Math.PI;
                start = from;
                delta = interior;
            }
            else if (angleType?.ToLowerInvariant() == "deflection")
            {
                double aIn = aPrev + Math.PI;
                start = aIn;
                delta = Normalize(aNext - aIn);
                extension = true;
                side = Math.Abs(delta) < 1e-9 ? String.Empty : (delta > 0 ? "L" : "R");
            }
            else
            {
                start = aNext;
                delta = Normalize(aPrev - aNext);
            }

            result.Add(new VertexAngle(c, radius, start, delta, Math.Abs(delta) * 180.0 / Math.PI, side, extension));
        }

        return result;
    }

    // draws the vertex angles: arcs, extensions and labels; angleUnit: none | deg | gon | both
    public void DrawVertexAngles(bool closed, string angleUnit, string angleType)
    {
        if (String.IsNullOrEmpty(angleUnit) || angleUnit.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var angle in VertexAngles(CalcPoints, closed, angleType))
        {
            if (angle.Extension)
            {
                DrawLine([angle.Center, At(angle.Center, angle.Radius * 1.5, angle.Start)], true);
            }
            if (angle.Delta != 0)
            {
                DrawArc(angle.Center, angle.Radius, angle.Start, angle.Start + angle.Delta);
            }

            var text = FormatAngle(angle.Degrees, angleUnit);
            if (!String.IsNullOrEmpty(angle.Side))
            {
                text = $"{text} {angle.Side}";
            }
            DrawLabel(At(angle.Center, angle.Radius * 1.4, angle.Start + angle.Delta / 2.0), text);
        }
    }

    #endregion

    public void Dispose()
    {
        _calcTransformer?.Dispose();
    }
}
