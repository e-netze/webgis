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

// shared drawing logic for the MapMarkup angle measure tools (ViewAngleElement, IntersectionAngleElement)
// the geometry must be the same as in the client: webgis-graphics.view_angle.js, webgis-graphics.intersection_angle.js
internal class AngleElementHelper : IDisposable
{
    private readonly IMap _map;
    private readonly ICanvas _canvas;
    private readonly GeometricTransformerPro _calcTransformer;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly float _fontSize;
    private readonly float _dpiFactor;

    public AngleElementHelper(ICanvas canvas, IMap map, Polyline polyline, Polyline calcPolyline, ArgbColor color, float width, float fontSize)
    {
        _canvas = canvas;
        _map = map;
        _color = color;
        _width = width;
        _fontSize = fontSize > 0 ? fontSize : 11f;
        _dpiFactor = map.DpiFactor();

        CalcPoints = SpatialAlgorithms.ShapePoints(calcPolyline ?? polyline, true);
        _calcTransformer = calcPolyline != null
            ? new GeometricTransformerPro(CoreApiGlobals.SRefStore, polyline.SrsId, calcPolyline.SrsId)
            : null;
    }

    public List<Point> CalcPoints { get; }

    public Point ToImage(double x, double y)
    {
        var point = new Point(x, y);
        _calcTransformer?.InvTransform(point);
        return _map.WorldToImage(point);
    }

    public void DrawLine(IEnumerable<Point> calcPoints, bool dashed)
    {
        var imagePoints = calcPoints.Select(p => ToImage(p.X, p.Y)).ToArray();
        if (imagePoints.Length < 2)
        {
            return;
        }

        using (var pen = Current.Engine.CreatePen(_color, _width * _dpiFactor))
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

    public void Dispose()
    {
        _calcTransformer?.Dispose();
    }
}
