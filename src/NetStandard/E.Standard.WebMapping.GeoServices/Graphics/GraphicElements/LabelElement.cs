using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class LabelElement : IGraphicElement
{
    private Point _point;
    private ArgbColor _color, _bkcolor;
    private float _size;
    private string _fontName, _text;

    public LabelElement(Point point, string text, ArgbColor color, ArgbColor bkcolor, string fontName, float size)
    {
        _point = point;
        _color = color;
        _bkcolor = bkcolor;
        _fontName = fontName;
        _size = size;
        _text = text;
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
            canvas.DrawOutlineLabel(map, _text, point, font, brush, bkbrush);
        }
    }

    #endregion
}
