using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;
using System.Linq;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class MeasurePolygonElement : PolygonElement
{
    private Polygon _calcPolygon;
    private bool _labelSegments = false;
    private float _fontSize = 11f;
    private Unit _areaUnit = Unit.Meter;
    private Unit _lengthUnit = Unit.Meter;

    //public MeasurePolygonElement(Polygon polygon, ArgbColor color, float width, LineDashStyle dashStyle = LineDashStyle.Solid, Polygon calcPolygon = null, bool labelSegments = false)
    //    : this(polygon, color, color, width, dashStyle, calcPolygon, labelSegments)
    //{
    //}
    public MeasurePolygonElement(
                Polygon polygon,
                ArgbColor penColor,
                ArgbColor brushColor,
                float width,
                LineDashStyle dashStyle = LineDashStyle.Solid,
                float fontSize = 11f,
                Polygon calcPolygon = null,
                bool labelSegments = false,
                string areaUnit = "m²")
        : base(polygon, penColor, brushColor, width, dashStyle, fontSize, areaUnit)
    {
        _fontSize = fontSize > 0 ? fontSize : 11f;
        _calcPolygon = calcPolygon;
        _labelSegments = labelSegments;
        _areaUnit = (!String.IsNullOrEmpty(areaUnit) ? areaUnit : "m²").FromUnitAbbreviation();
        _lengthUnit = _areaUnit switch
        {
            Unit.Kilometer => Unit.Kilometer,
            _ => Unit.Meter
        };
    }

    #region IGraphicElement Member

    public override void Draw(ICanvas canvas, IMap map)
    {
        float dpiFactor = map.DpiFactor();
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
                    using (var font = Current.Engine.CreateFont(Platform.SystemInfo.DefaultFontName, _fontSize * dpiFactor))
                    {
                        double area = calcPolygon.Area;  // [m²]
                        double circumference = calcPolygon.Rings.Select(r => r.Length).Sum();  // [m]

                        area = area.SquareMetersToSquareUnit(_areaUnit);
                        circumference = circumference.MetersToUnit(_lengthUnit);
                        
                        var text = $"F: {Math.Round(area, 2)}{_areaUnit.ToSquareAbbreviation()}{System.Environment.NewLine}U: {Math.Round(circumference, 2)}{_lengthUnit.ToAbbreviation()}{System.Environment.NewLine}EPSG:{(_calcPolygon ?? base.Polygon).SrsId}";
                        var box = canvas.MeasureText(text, font);

                        point.X -= box.Width / 2f;
                        point.Y -= box.Height / 1.1f;

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
