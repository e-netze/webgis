using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class MeasurePolygonElement : PolygonElement
{
    private Polygon _calcPolygon;
    private bool _labelSegments = false;

    public MeasurePolygonElement(Polygon polygon, ArgbColor color, float width, LineDashStyle dashStyle = LineDashStyle.Solid, Polygon calcPolygon = null, bool labelSegments = false)
        : this(polygon, color, color, width, dashStyle, calcPolygon, labelSegments)
    {
    }
    public MeasurePolygonElement(Polygon polygon, ArgbColor penColor, ArgbColor brushColor, float width, LineDashStyle dashStyle = LineDashStyle.Solid, Polygon calcPolygon = null, bool labelSegments = false)
        : base(polygon, penColor, brushColor, width, dashStyle)
    {
        _calcPolygon = calcPolygon;
        _labelSegments = labelSegments;
    }

    #region IGraphicElement Member

    public override void Draw(ICanvas canvas, IMap map)
    {
        base.Draw(canvas, map, _labelSegments, _labelSegments, _calcPolygon);

        try
        {
            var calcPolygon = _calcPolygon ?? base.Polygon;

            if (calcPolygon != null && calcPolygon.Area > 0.0)
            {
                var point = map.WorldToImage(base.Polygon.ShapeEnvelope.CenterPoint);

                try
                {
                    using (var rectPen = Current.Engine.CreatePen(ArgbColor.FromArgb(120, 120, 129), 1))
                    using (var rectBrush = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(150, 255, 255, 0)))
                    using (var fontBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
                    using (var font = Current.Engine.CreateFont(Platform.SystemInfo.DefaultFontName, 11))
                    //using (var graphicsPath = Current.Engine.CreateGraphicsPath())
                    {
                        var text = $"F: {Math.Round(calcPolygon.Area, 2)}m²{System.Environment.NewLine}U: {Math.Round(calcPolygon.Rings.Select(r => r.Length).Sum(), 2)}m{System.Environment.NewLine}{System.Environment.NewLine}EPSG:{(_calcPolygon ?? base.Polygon).SrsId}";
                        var box = canvas.MeasureText(text, font);
                        canvas.FillRectangle(rectBrush, new CanvasRectangleF((float)point.X, (float)point.Y, box.Width, box.Height));
                        canvas.DrawRectangle(rectPen, new CanvasRectangleF((float)point.X, (float)point.Y, box.Width, box.Height));

                        var format = Current.Engine.CreateDrawTextFormat();
                        format.Alignment = StringAlignment.Near;
                        format.LineAlignment = StringAlignment.Near;

                        canvas.DrawText(text, font, fontBrush, new CanvasPointF((float)point.X, (float)point.Y), format);
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    #endregion
}
