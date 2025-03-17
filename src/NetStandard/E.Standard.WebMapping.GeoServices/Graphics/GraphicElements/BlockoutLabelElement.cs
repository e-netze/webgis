using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class BlockoutLabelElement : IGraphicElement
{
    private readonly Point _point;
    private readonly ArgbColor _color, _bkcolor;
    private readonly float _size;
    private readonly string _fontName, _text;
    private readonly Offset _offset;

    public BlockoutLabelElement(Point point, string text, ArgbColor color, ArgbColor bkcolor, string fontName, float size, Offset offset)
    {
        _point = point;
        _color = color;
        _bkcolor = bkcolor;
        _fontName = fontName;
        _size = size;
        _text = text;
        _offset = offset ?? new Offset(0, 0);
    }

    #region IGraphicElement Member

    public void Draw(ICanvas canvas, IMap map)
    {
        Point point = map.WorldToImage(_point);

        using (var brush = Current.Engine.CreateSolidBrush(_color))
        using (var bkbrush = Current.Engine.CreateSolidBrush(_bkcolor))
        using (var font = Current.Engine.CreateFont(_fontName, _size))
        {
            var oldHint = canvas.TextRenderingHint;
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            var sizeF = canvas.MeasureText(_text, font);
            var rectF = new CanvasRectangleF(
                (float)point.X /*- sizeF.Width / 2f*/, (float)point.Y - sizeF.Height /*- sizeF.Height / 2f*/, sizeF.Width, sizeF.Height);

            canvas.TranslateTransform(new CanvasPointF(_offset.Left, _offset.Top));

            if (_bkcolor.A > 0)
            {
                canvas.FillRectangle(bkbrush, rectF);
            }

            var format = Current.Engine.CreateDrawTextFormat();
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;

            canvas.DrawText(_text, font, brush, rectF.Center, format);

            canvas.ResetTransform();

            canvas.TextRenderingHint = oldHint;
        }
    }

    #endregion
}
