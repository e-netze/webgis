using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class MapFrameElement : IGraphicElement
{
    private readonly Point _center;
    private readonly double _width;
    private readonly double _height;
    private readonly double _rotation;
    private readonly ArgbColor _borderColor;
    private readonly string _name;

    public MapFrameElement(string name,
        Point center,
        double width, double height,
        double rotation,
        ArgbColor borderColor)
    {
        _name = name;
        _center = center;
        _width = width;
        _height = height;
        _rotation = rotation;
        _borderColor = borderColor;
    }

    #region IGraphicElement

    public void Draw(ICanvas canvas, IMap map)
    {
        var ring = CalcPolygonRing();

        using var pen = Current.Engine.CreatePen(_borderColor, 2);
        var imgPoints = ring.ToArray().Select(p => map.WorldToImage(p)).ToArray();
        var path = Current.Engine.CreateGraphicsPath();

        path.StartFigure();
        for (int i = 0; i < imgPoints.Length - 1; i++)
        {
            path.AddLine((float)imgPoints[i].X, (float)imgPoints[i].Y, (float)imgPoints[i + 1].X, (float)imgPoints[i + 1].Y);
        }
        path.CloseFigure();
        canvas.DrawPath(pen, path);

        DrawLabel(canvas, map, imgPoints[0], 1.0f, 1.0f);
        DrawLabel(canvas, map, imgPoints[1], -1.0f, 1.0f);
        DrawLabel(canvas, map, imgPoints[2], -1.0f, -1.0f);
        DrawLabel(canvas, map, imgPoints[3], 1.0f, -1.0f);
    }

    public Envelope Extent =>
        _center is not null
            ? new Envelope(CalcPolygonRing().ShapeEnvelope)
            : null;

    #endregion

    private Ring CalcPolygonRing()
    {
        var ring = new Ring();
        ring.AddPoints(new Point[] {
                new Point(-_width/2.0, _height/2.0),
                new Point(_width/2.0, _height/2.0),
                new Point(_width/2.0, -_height/2.0),
                new Point(-_width/2.0, -_height/2.0),
                new Point(-_width/2.0, _height/2.0)
            });
        double angle = -_rotation * Math.PI / 180.0;
        double cosA = Math.Cos(angle), sinA = Math.Sin(angle);

        for (int i = 0; i < ring.PointCount; i++)
        {
            var p = ring[i];
            var x = p.X * cosA - p.Y * sinA + _center.X;
            var y = p.X * sinA + p.Y * cosA + _center.Y;
            p.X = x; p.Y = y;
        }

        return ring;
    }

    private void DrawLabel(ICanvas canvas, IMap map, Point p, float scaleX, float scaleY)
    {
        try
        {
            canvas.TranslateTransform(new CanvasPointF((float)p.X, (float)p.Y));
            canvas.RotateTransform((float)(_rotation - map.DisplayRotation));

            using var brush = Current.Engine.CreateSolidBrush(ArgbColor.Black);
            using var textBrush = Current.Engine.CreateSolidBrush(ArgbColor.White);
            using var font = Current.Engine.CreateFont("Arial", 10, FontStyle.Regular);

            var size = canvas.MeasureText(_name, font);

            var rect = new CanvasRectangleF(
                scaleX < 0 ?  size.Width * scaleX : 0f, 
                scaleY < 0 ?  size.Height * scaleY : 0f,
                size.Width, 
                size.Height);

            canvas.FillRectangle(brush, rect);
            canvas.DrawText(_name, font, textBrush, rect);
        }
        finally
        {
            canvas.ResetTransform();
        }
    }
}
