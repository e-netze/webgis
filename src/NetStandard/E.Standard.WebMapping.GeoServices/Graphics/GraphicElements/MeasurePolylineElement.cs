using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using System;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

public class MeasurePolylineElement : PolylineElement
{
    private Polyline _calcPolyline;
    private bool _labelSegments = false;
    private bool _labelPointNumbers = false;
    private bool _labelTotalLength = true;
    private float _fontSize;
    private Unit _lengthUnit = Unit.Meter;

    public MeasurePolylineElement(Polyline polyline,
                                 ArgbColor color,
                                 float width,
                                 LineDashStyle dashStyle = LineDashStyle.Solid,
                                 Polyline calcPolyline = null,
                                 bool labelSegments = false,
                                 bool labelPointNumbers = false,
                                 bool labelTotalLength = true,
                                 float fontSize = 11f,
                                 string lengthUnit = "m")
        : base(polyline, color, width, dashStyle, fontSize, lengthUnit)
    {
        _calcPolyline = calcPolyline;
        _labelSegments = labelSegments;
        _labelPointNumbers = labelPointNumbers;
        _labelTotalLength = labelTotalLength;
        _fontSize = fontSize > 0 ? fontSize : 11f;
        _lengthUnit = (!String.IsNullOrEmpty(lengthUnit) ? lengthUnit : "m").FromUnitAbbreviation();
    }

    #region IGraphicElement Member

    override public void Draw(ICanvas canvas, IMap map)
    {
        base.Draw(canvas, map, _labelPointNumbers, _labelSegments, _calcPolyline);
        float dpiFactor = map.DpiFactor();

        if (_labelTotalLength)
        {
            try
            {
                if (base.Polyline != null)
                {

                    for (int i = 0; i < base.Polyline.PathCount; i++)
                    {
                        Path path = base.Polyline[i];
                        Path calcPath = (_calcPolyline ?? base.Polyline)[i];

                        if (path.PointCount == 0)
                        {
                            continue;
                        }

                        var point = map.WorldToImage(path[path.PointCount - 1]);

                        try
                        {
                            using (var rectPen = Current.Engine.CreatePen(ArgbColor.FromArgb(120, 120, 129), 1))
                            using (var rectBrush = Current.Engine.CreateSolidBrush(ArgbColor.FromArgb(150, 255, 255, 0)))
                            using (var fontBrush = Current.Engine.CreateSolidBrush(ArgbColor.Black))
                            using (var font = Current.Engine.CreateFont(Platform.SystemInfo.DefaultFontName, _fontSize * dpiFactor))
                            {
                                var length = calcPath.Length.MetersToUnit(_lengthUnit);
                                var text = $"∑: {Math.Round(length, 2)}{_lengthUnit.ToAbbreviation()}{System.Environment.NewLine}EPSG:{(_calcPolyline ?? base.Polyline).SrsId}";
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
            }
            catch { }
        }
    }

    #endregion
}
