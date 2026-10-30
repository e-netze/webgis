using System;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// MapMarkup measure tool 'view_angle' (Sichtfeld): vertex 0 => apex, vertex 1 => end of the axis
// same geometry as webgis-graphics.view_angle.js
public class ViewAngleElement : IGraphicElement
{
    private readonly Polyline _polyline, _calcPolyline;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly double _angle;
    private readonly string _unit;
    private readonly bool _labelLength;
    private readonly float _fontSize;

    public ViewAngleElement(Polyline polyline,
                            ArgbColor color,
                            float width,
                            double angle = 30.0,
                            string unit = "deg",
                            bool labelLength = false,
                            float fontSize = 11f,
                            Polyline calcPolyline = null)
    {
        _polyline = polyline;
        _color = color;
        _width = width;
        _angle = angle > 0 && angle < 360 ? angle : 30.0;
        _unit = unit;
        _labelLength = labelLength;
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

            Point a = points[0], b = points[1];
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len == 0)
            {
                return;
            }

            double theta = Math.Atan2(dy, dx), half = _angle / 2.0 * Math.PI / 180.0;

            helper.DrawLine([a, b], false);
            helper.DrawLine([a, AngleElementHelper.At(a, len, theta + half)], true);
            helper.DrawLine([a, AngleElementHelper.At(a, len, theta - half)], true);

            helper.DrawArc(a, len * 0.6, theta - half, theta + half);
            helper.DrawArc(a, len * 0.8, theta - half, theta);
            helper.DrawArc(a, len * 0.8, theta, theta + half);

            helper.DrawLabel(AngleElementHelper.At(a, len * 0.5, theta), AngleElementHelper.FormatAngle(_angle, _unit));
            helper.DrawLabel(AngleElementHelper.At(a, len * 0.85, theta + half / 2.0), AngleElementHelper.FormatAngle(_angle / 2.0, _unit));
            helper.DrawLabel(AngleElementHelper.At(a, len * 0.85, theta - half / 2.0), AngleElementHelper.FormatAngle(_angle / 2.0, _unit));

            if (_labelLength)
            {
                helper.DrawLabel(b, $"{AngleElementHelper.FormatNumber(len)} m", alignNear: true);
            }
        }
    }

    public Envelope Extent =>
        _polyline is not null
            ? new Envelope(_polyline.ShapeEnvelope)
            : null;
}
