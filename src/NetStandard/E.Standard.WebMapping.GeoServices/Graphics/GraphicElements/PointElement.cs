using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class PointElement : IGraphicElement
{
    private Point _point;
    private ArgbColor _color1;
    private float _width = 1;

    public PointElement(Point point, ArgbColor brushColor, float width)
    {
        _point = point;
        _color1 = brushColor;
        _width = width;
    }

    #region IGraphicElement Member

    public void Draw(ICanvas canvas, IMap map)
    {
        Point point = map.WorldToImage(_point);

        using (var brush = Current.Engine.CreateSolidBrush(_color1))
        {
            canvas.FillEllipse(brush, (float)point.X - _width / 2, (float)point.Y - _width / 2, _width, _width);
        }
    }

    public Envelope Extent =>
        _point is not null
            ? new Envelope(_point.ShapeEnvelope)
            : null;

    #endregion
}
