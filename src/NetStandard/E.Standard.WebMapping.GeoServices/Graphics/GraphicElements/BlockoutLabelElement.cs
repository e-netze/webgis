using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
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
        using (var outlinebrush = Current.Engine.CreateSolidBrush(_bkcolor))
        using (var boxBrush = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(_bkcolor.A / 5, _bkcolor)))
        using (var boxPen = Current.Engine.CreatePen(ArgbColor.FromArgb((int)(_bkcolor.A / 2f), _bkcolor), 1.5f))
        using (var font = Current.Engine.CreateFont(_fontName, _size))
        {
            var oldHint = canvas.TextRenderingHint;
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            var sizeF = canvas.MeasureText(_text, font);
            var rectF = new CanvasRectangleF(
                (float)point.X /*- sizeF.Width / 2f*/- 1f, (float)point.Y - sizeF.Height - 1f /*- sizeF.Height / 2f*/, sizeF.Width + 2f, sizeF.Height + 2f);

            canvas.TranslateTransform(new CanvasPointF(_offset.Left, _offset.Top));

            if (_bkcolor.A > 0)
            {
                canvas.FillRectangle(boxBrush, rectF);
                canvas.DrawRectangle(boxPen, rectF);
            }

            var format = Current.Engine.CreateDrawTextFormat();
            format.Alignment = StringAlignment.Near;
            format.LineAlignment = StringAlignment.Near;

            var centerPoint = new Point(rectF.Left + 1f, rectF.Top + 1.5f);
            canvas.DrawOutlineLabel(map, _text, centerPoint, font, brush, outlinebrush, format);

            canvas.ResetTransform();

            canvas.TextRenderingHint = oldHint;
        }
    }

    #endregion
}
