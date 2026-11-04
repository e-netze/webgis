using System;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class LabelElement : IGraphicElement
{
    private Point _point;
    private ArgbColor _color, _bkcolor;
    private float _size;
    private string _fontName, _text;
    private float _rotation;

    public LabelElement(Point point, string text, ArgbColor color, ArgbColor bkcolor, string fontName, float size, float rotation = 0f)
    {
        _point = point;
        _color = color;
        _bkcolor = bkcolor;
        _fontName = fontName;
        _size = size;
        _text = text;
        _rotation = rotation;
    }

    #region IGraphicElement Member

    public void Draw(ICanvas canvas, IMap map)
    {
        if (String.IsNullOrEmpty(_text))
        {
            return;
        }

        Point point = map.WorldToImage(_point);

        var size = _size * (float)map.Dpi / 96f;

        using (var brush = Current.Engine.CreateSolidBrush(_color))
        using (var bkbrush = Current.Engine.CreateSolidBrush(_bkcolor))
        using (var font = Current.Engine.CreateFont(_fontName, size))
        {
            if (_rotation % 360f == 0f)
            {
                canvas.DrawOutlineLabel(map, _text, point, font, brush, bkbrush);
                return;
            }

            try
            {
                canvas.TranslateTransform(new CanvasPointF((float)point.X, (float)point.Y));
                canvas.RotateTransform(ImageAngle(map));
                canvas.DrawOutlineLabel(map, _text, new Point(0, 0), font, brush, bkbrush);
            }
            finally
            {
                canvas.ResetTransform();
            }
        }
    }

    // rotation is clockwise relative to the map (north up) => image angle, respects map.DisplayRotation
    private float ImageAngle(IMap map)
    {
        double rad = _rotation * Math.PI / 180.0;
        double d = map.Extent is not null && map.Extent.Width > 0 ? map.Extent.Width / 100.0 : 1.0;

        Point p0 = map.WorldToImage(_point),
              p1 = map.WorldToImage(new Point(_point.X + Math.Cos(rad) * d, _point.Y - Math.Sin(rad) * d));

        return (float)(Math.Atan2(p1.Y - p0.Y, p1.X - p0.X) * 180.0 / Math.PI);
    }

    public Envelope Extent =>
        _point is not null
            ? new Envelope(_point.ShapeEnvelope)
            : null;

    #endregion
}
