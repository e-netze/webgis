using System;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// MapMarkup measure tool 'azimuth' (Richtungswinkel), polyline/traverse: azimuth for each segment
// the azimuth is measured clockwise from (grid) north of the calc crs
// same geometry as webgis-graphics.azimuth.js
public class AzimuthElement : IGraphicElement
{
    private readonly Polyline _polyline, _calcPolyline;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly string _unit;
    private readonly bool _labelLength;
    private readonly Unit _lengthUnit;
    private readonly float _fontSize;

    public AzimuthElement(Polyline polyline,
                          ArgbColor color,
                          float width,
                          string unit = "deg",
                          bool labelLength = true,
                          string lengthUnit = "m",
                          float fontSize = 11f,
                          Polyline calcPolyline = null)
    {
        _polyline = polyline;
        _color = color;
        _width = width;
        _unit = unit;
        _labelLength = labelLength;
        _lengthUnit = (!String.IsNullOrEmpty(lengthUnit) ? lengthUnit : "m").FromUnitAbbreviation();
        _fontSize = fontSize;
        _calcPolyline = calcPolyline;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        using (var helper = new AngleElementHelper(canvas, map, _polyline, _calcPolyline, _color, _width, _fontSize))
        {
            var points = helper.CalcPoints;
            if (points.Count < 2)
            {
                return;
            }

            helper.DrawLine(points, false);

            double north = Math.PI / 2.0;
            for (int i = 1; i < points.Count; i++)
            {
                Point a = points[i - 1], b = points[i];
                double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
                if (len == 0)
                {
                    continue;
                }

                double azimuth = Azimuth(dx, dy), azimuthRad = azimuth * Math.PI / 180.0;

                helper.DrawLine([a, AngleElementHelper.At(a, len * 0.25, north)], true);
                helper.DrawArc(a, len * 0.15, north - azimuthRad, north);

                helper.DrawLabel(AngleElementHelper.At(a, len * 0.22, north - azimuthRad / 2.0), AngleElementHelper.FormatAngle(azimuth, _unit));

                if (_labelLength)
                {
                    helper.DrawSegmentLength(a, b, len, _lengthUnit);
                }
            }
        }
    }

    static public double Azimuth(double dx, double dy)
    {
        double azimuth = 90.0 - Math.Atan2(dy, dx) * 180.0 / Math.PI;
        azimuth %= 360.0;
        return azimuth < 0 ? azimuth + 360.0 : azimuth;
    }

    public Envelope Extent =>
        _polyline is not null
            ? new Envelope(_polyline.ShapeEnvelope)
            : null;
}
