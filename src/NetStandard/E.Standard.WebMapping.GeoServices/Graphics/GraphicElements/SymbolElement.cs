using E.Standard.Platform;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class SymbolElement : IGraphicElement
{
    private Point _point;
    private string _symbolPath, _label;
    private float _hotspotX = 0, _hotspotY = 0;

    public SymbolElement(Point point, string symbolPath, float hotspotX = -1f, float hotspotY = -1f, string label = "")
    {
        _point = point;
        _symbolPath = symbolPath;
        _hotspotX = hotspotX;
        _hotspotY = hotspotY;
        _label = label;
    }

    #region IGraphicElement Member

    public void Draw(ICanvas canvas, IMap map)
    {
        Point point = map.WorldToImage(_point);
        try
        {
            using (var symbol = Current.Engine.CreateBitmap(_symbolPath))
            {
                float width = symbol.Width, height = symbol.Height;
                _hotspotX = _hotspotX >= 0 ? _hotspotX : width / 2;
                _hotspotY = _hotspotY >= 0 ? _hotspotY : height / 2;

                float dpiFactor = (float)map.Dpi / 96f;

                canvas.DrawBitmap(
                        symbol,
                        new CanvasRectangleF((float)point.X - _hotspotX * dpiFactor, (float)point.Y - _hotspotY * dpiFactor, width * dpiFactor, height * dpiFactor),
                        new CanvasRectangleF(0, 0, symbol.Width, symbol.Height)
                    );

                if (!String.IsNullOrEmpty(_label))
                {
                    var hint = canvas.TextRenderingHint;
                    canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

                    using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 10 * dpiFactor))
                    using (var whiteBrush = Current.Engine.CreateSolidBrush(ArgbColor.White))
                    using (var blackBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
                    {
                        var size = canvas.MeasureText(_label, font);
                        var txtPoint = new CanvasPointF((float)point.X, (float)point.Y - _hotspotY * dpiFactor - size.Height);

                        for (var x = -1; x <= 1; x++)
                        {
                            for (var y = -1; y <= 1; y++)
                            {
                                canvas.DrawText(_label, font, whiteBrush, new CanvasPointF(txtPoint.X - x, txtPoint.Y - y));
                            }
                        }

                        canvas.DrawText(_label, font, blackBrush, txtPoint);
                    }

                    canvas.TextRenderingHint = hint;
                }
            }
        }
        catch { }
    }

    #endregion
}
