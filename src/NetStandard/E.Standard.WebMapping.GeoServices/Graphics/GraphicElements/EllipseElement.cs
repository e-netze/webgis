using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class EllipseElement : IGraphicElement
{
    private Point _center;
    private double _radiusX, _radiusY;
    private ArgbColor _color1, _color2;
    private float _width = 1;
    private LineDashStyle _dashStyle;

    public enum Unit
    {
        World = 0,
        Pixel = 1
    }

    private Unit _unit = Unit.World;

    public EllipseElement(Point center,
                          double radiusX, double radiusY,
                          ArgbColor color,
                          float width,
                          LineDashStyle dashStyle = LineDashStyle.Solid,
                          Unit unit = Unit.World)
        : this(center, radiusX, radiusY, color, color, width, dashStyle, unit)
    {
    }
    public EllipseElement(Point center,
                          double radiusX, double radiusY,
                          ArgbColor penColor,
                          ArgbColor brushColor,
                          float width,
                          LineDashStyle dashStyle = LineDashStyle.Solid,
                          Unit unit = Unit.World)
    {
        _center = center;
        _radiusX = radiusX;
        _radiusY = radiusY;
        _color1 = penColor;
        _color2 = brushColor;
        _width = width;
        _dashStyle = dashStyle;
        _unit = unit;
    }


    #region IGraphicElement

    public void Draw(ICanvas canvas, IMap map)
    {
        try
        {
            using (var pen = Current.Engine.CreatePen(_color1, _width))
            using (var brush = Current.Engine.CreateSolidBrush(_color2))
            {
                pen.DashStyle = _dashStyle;
                pen.StartCap = pen.EndCap = LineCap.Round;

                if (_radiusX.Equals(_radiusY) && _radiusX >= 50000)
                {
                    using (var gp = CircleToPath(_center, _radiusX, map))
                    {
                        canvas.FillPath(brush, gp);
                        canvas.DrawPath(pen, gp);
                    }
                }
                else
                {
                    Point point = map.WorldToImage(_center);

                    float rX, rY;

                    switch (_unit)
                    {
                        case Unit.Pixel:
                            rX = (float)_radiusX * (float)(map.Dpi / 96f);
                            rY = (float)_radiusY * (float)(map.Dpi / 96f);
                            break;
                        default:  // Map Units
                            rX = (float)point.Distance2D(map.WorldToImage(new Point(_center.X + _radiusX, _center.Y)));
                            rY = (float)point.Distance2D(map.WorldToImage(new Point(_center.X, _center.Y + _radiusY)));
                            break;
                    }

                    canvas.FillEllipse(brush, (float)point.X - rX, (float)point.Y - rY, rX * 2f, rY * 2f);
                    canvas.DrawEllipse(pen, (float)point.X - rX, (float)point.Y - rY, rX * 2f, rY * 2f);
                }
            }
        }
        catch { }
    }

    public Envelope Extent =>
        _center is not null
            ? new Envelope(
                _center.X - _radiusX, _center.Y - _radiusY,
                _center.X + _radiusX, _center.Y + _radiusY)
            : null;

    #endregion

    #region Static Members

    public static IGraphicsPath CircleToPath(Point center, double radius, IMap map, double stepWidth = 0.01)  // ToDO: Strittweite: 0.01??
        => CircleToPathPro(center, radius, map, stepWidth).Path;

    public static GraphicsPathPro CircleToPathPro(Point center, double radius, IMap map, double stepWidth = 0.01)  // ToDO: Strittweite: 0.01??
    {
        if (map.SpatialReference == null)
        {
            throw new ArgumentException("A spatialreference for the map is required to create an ellipse path");
        }

        var sRef4326 = CoreApiGlobals.SRefStore.SpatialReferences.ById(4326);

        center = new Point(center);
        using (var transformer = new GeometricTransformerPro(map.SpatialReference, sRef4326))
        {
            transformer.Transform(center);

            double alpha = radius / 6371000, lat = 90.0 - center.Y, lng = 90.0 - center.X;

            double sin_a = Math.Sin(alpha), cos_a = Math.Cos(alpha);
            double sin_lat = Math.Sin(lat * Math.PI / 180.0), cos_lat = Math.Cos(lat * Math.PI / 180.0);
            double sin_lng = Math.Sin(lng * Math.PI / 180.0), cos_lng = Math.Cos(lng * Math.PI / 180.0);

            var first = true;
            float prevX = 0f, prevY = 0f;

            var graphicPath = Current.Engine.CreateGraphicsPath();
            var canvasPoints = new List<CanvasPointF>();

            for (double t = 0; t < Math.PI * 2.0; t += stepWidth)
            {
                // https://math.stackexchange.com/questions/643130/circle-on-sphere

                double x = (sin_a * cos_lat * cos_lng) * Math.Cos(t) + (sin_a * sin_lng) * Math.Sin(t) - (cos_a * sin_lat * cos_lng);
                double y = -(sin_a * cos_lat * sin_lng) * Math.Cos(t) + (sin_a * cos_lng) * Math.Sin(t) + (cos_a * sin_lat * sin_lng);
                double z = (sin_a * sin_lat) * Math.Cos(t) + cos_a * cos_lat;

                double lat_ = Math.Asin(z) * 180.0 / Math.PI;
                double lng_ = -Math.Atan2(x, y) * 180.0 / Math.PI;

                var point = new Point(lng_, lat_);
                transformer.InvTransform(point);

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
    }

    #endregion
}
