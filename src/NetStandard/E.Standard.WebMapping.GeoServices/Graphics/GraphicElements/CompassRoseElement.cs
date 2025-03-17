using E.Standard.Platform;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class CompassRoseElement : IGraphicElement
{
    private Point _center;
    private double _radius;
    private int _steps;
    private ArgbColor _color;
    private float _width = 1;


    public CompassRoseElement(
            Point center,
            double radius,
            int steps,
            ArgbColor penColor,
            float width)
    {
        _center = center;
        _radius = radius;
        _steps = steps;
        _color = penColor;
        _width = width;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        try
        {
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            using (var pen = Current.Engine.CreatePen(_color, _width))
            {
                // outer ring
                using (var rasterPath = CirclePath(map, _center, _radius, 360))
                {
                    canvas.DrawPath(pen, rasterPath.Path);
                }
                // inner ring
                using (var rasterPath = CirclePath(map, _center, _radius / 10.0, 360))
                {
                    canvas.DrawPath(pen, rasterPath.Path);
                }
                // inner lines
                using (var outerPath = CirclePath(map, _center, _radius, 8))
                using (var innerPath = CirclePath(map, _center, _radius / 10.0, 8))
                {
                    var labels = new string[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
                    for (int i = 0; i < 8; i++)
                    {
                        var p1 = outerPath.Points[i];
                        var p2 = innerPath.Points[i];

                        canvas.DrawLine(pen, p1, p2);

                        DrawString(canvas, map, (p1.X + p2.X) * .5f, (p1.Y + p2.Y) * .5f, labels[i]);
                    }
                }
                // outer lines
                using (var outerPath = CirclePath(map, _center, _radius * 1.2, _steps))
                using (var innerPath = CirclePath(map, _center, _radius, _steps))
                {
                    for (int i = 0; i < _steps; i++)
                    {
                        var p1 = outerPath.Points[i];
                        var p2 = innerPath.Points[i];

                        canvas.DrawLine(pen, p1, p2);
                        DrawString(canvas, map, p1.X, p1.Y, $"{(int)(i * 360.0 / _steps)}°");
                    }
                }
            }
        }
        catch { }
        finally
        {
            canvas.ResetTransform();
        }
    }

    #region Helper

    private GraphicsPathPro CirclePath(IMap map, Point center, double radius, double steps, bool close = false)
    {
        var graphicPath = Current.Engine.CreateGraphicsPath();
        var canvasPoints = new List<CanvasPointF>();

        var first = true;
        float prevX = 0f, prevY = 0f;
        for (int i = 0; i < steps + (close ? 1 : 0); i++)
        {
            double w = 2.0 * Math.PI / steps * i;
            var point = new Point(center.X + Math.Sin(w) * radius, center.Y + Math.Cos(w) * radius);

            var canvasPoint = map.WorldToImage(point);
            canvasPoints.Add(new CanvasPointF((float)canvasPoint.X, (float)canvasPoint.Y));

            if (!first)
            {
                graphicPath.AddLine(prevX, prevY, (float)canvasPoint.X, (float)canvasPoint.Y);
            }
            else
            {
                first = false;
            }

            prevX = (float)canvasPoint.X;
            prevY = (float)canvasPoint.Y;
        }

        return new GraphicsPathPro(graphicPath, canvasPoints.ToArray());

    }

    private float FontSize(IMap map) => 12f * map.DpiFactor() * Platform.SystemInfo.FontSizeFactor;

    private void DrawString(ICanvas canvas, IMap map, float x, float y, string text)
    {
        if (String.IsNullOrEmpty(text))
        {
            return;
        }

        using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, FontSize(map)))
        using (var whiteBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
        using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
        {
            var stringFormat = Current.Engine.CreateDrawTextFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            canvas.DrawOutlineLabel(map, text, new Point(x, y), font, blackBrush, whiteBrush, stringFormat);
        }
    }

    #endregion
}
