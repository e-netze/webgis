using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class DistanceCircleElement : IGraphicElement
{
    private Point _center;
    private double _radius;
    private int _steps;
    private ArgbColor _color1, _color2;
    private float _width = 1;
    private LineDashStyle _dashStyle;

    public DistanceCircleElement(Point center, double radius, int steps, ArgbColor color, float width, LineDashStyle dashStyle = LineDashStyle.Solid)
        : this(center, radius, steps, color, color, width, dashStyle)
    {
    }
    public DistanceCircleElement(Point center, double radius, int steps, ArgbColor penColor, ArgbColor brushColor, float width, LineDashStyle dashStyle = LineDashStyle.Solid)
    {
        _center = center;
        _radius = radius;
        _steps = steps;
        _color1 = penColor;
        _color2 = brushColor;
        _width = width;
        _dashStyle = dashStyle;
    }


    #region IGraphicElement

    public void Draw(ICanvas canvas, IMap map)
    {
        try
        {
            Point point = map.WorldToImage(_center);

            float rX = (float)point.Distance2D(map.WorldToImage(new Point(_center.X + _radius, _center.Y)));
            float rY = (float)point.Distance2D(map.WorldToImage(new Point(_center.X, _center.Y + _radius)));

            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            using (var rasterPath = EllipseElement.CircleToPathPro(_center, _radius, map, Math.PI / 4.0))
            {
                canvas.ResetTransform();
                using (var brush = Current.Engine.CreateSolidBrush(_color2))
                {
                    for (int i = _steps; i >= 0; i--)
                    {

                        double radius = (_radius / _steps) * i;

                        using (var gp = EllipseElement.CircleToPath(_center, radius, map))
                        {
                            canvas.FillPath(brush, gp);
                        }
                    }
                }

                using (var pen = Current.Engine.CreatePen(ArgbColor.Gray, 1f))
                {
                    List<GraphicsPathPro> rasterPaths = new List<GraphicsPathPro>();
                    for (int i = 1; i <= _steps; i++)
                    {
                        double radius = (_radius / _steps) * i;
                        if (radius > 0)
                        {
                            rasterPaths.Add(EllipseElement.CircleToPathPro(_center, radius, map, Math.PI / 4.0));
                        }
                    }

                    for (var p = 0; p < rasterPaths[0].Points.Length; p++)
                    {
                        float prevX = float.NaN, prevY = float.NaN;
                        foreach (var rPath in rasterPaths)
                        {
                            canvas.DrawLine(pen,
                                float.IsNaN(prevX) ? (float)point.X : prevX,
                                float.IsNaN(prevY) ? (float)point.Y : prevY,
                                rPath.Points[p].X,
                                rPath.Points[p].Y);

                            prevX = rPath.Points[p].X;
                            prevY = rPath.Points[p].Y;
                        }
                    }

                    foreach (var rPath in rasterPaths)
                    {
                        rPath.Dispose();
                    }
                    rasterPaths.Clear();
                }

                canvas.ResetTransform();
                using (var pen = Current.Engine.CreatePen(_color1, _width))
                {
                    for (int i = _steps; i >= 0; i--)
                    {

                        pen.DashStyle = _dashStyle;
                        pen.StartCap = pen.EndCap = LineCap.Round;

                        double radius = (_radius / _steps) * i;

                        using (var gp = EllipseElement.CircleToPath(_center, radius, map))
                        {
                            canvas.DrawPath(pen, gp);
                        }
                    }
                }

                float fontSize = FontSize(map);

                DrawString(canvas, map, rasterPath.Points[2].X - fontSize, rasterPath.Points[2].Y, "W");
                DrawString(canvas, map, rasterPath.Points[6].X + fontSize, rasterPath.Points[6].Y, "E");
                DrawString(canvas, map, rasterPath.Points[4].X, rasterPath.Points[4].Y + fontSize, "S");
                DrawString(canvas, map, rasterPath.Points[0].X, rasterPath.Points[0].Y - fontSize, "N");


                for (int i = 1; i <= _steps; i++)
                {
                    double radius = (_radius / _steps) * i;

                    string label = Math.Round(radius, 2) + "m";
                    if (radius > 1000)
                    {
                        label = Math.Round(radius / 1000.0, 1) + "km";
                    }

                    using (var gp = EllipseElement.CircleToPathPro(_center, radius, map, Math.PI / 4.0))
                    {
                        canvas.TranslateTransform(new CanvasPointF(gp.Points[7].X, gp.Points[7].Y - FontSize(map)));
                        canvas.RotateTransform(45f);
                        DrawString(canvas, map, 0f, 0f, label);
                        canvas.ResetTransform();
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

    #endregion

    #region Helper

    private float FontSize(IMap map) => 12f * map.DpiFactor() * Platform.SystemInfo.FontSizeFactor;

    private void DrawString(ICanvas canvas, IMap map, float x, float y, string text)
    {
        if (String.IsNullOrEmpty(text))
        {
            return;
        }

        using (var font = Current.Engine.CreateFont(Platform.SystemInfo.DefaultFontName, FontSize(map)))
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
