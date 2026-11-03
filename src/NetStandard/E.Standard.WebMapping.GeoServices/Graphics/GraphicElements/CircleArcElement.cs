using System;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// MapMarkup measure tool 'circle_arc' (Kreisbogen): vertices 0,1,2 => start, intermediate point, end of the arc
// same geometry as webgis-graphics.circle_arc.js (calculation: CircleArcCalculator)
public class CircleArcElement : IGraphicElement
{
    private const double CrossFactor = 0.1, RadiusLabelFactor = 0.6, AngleLabelFactor = 0.25;  // see L.CircleArc

    private readonly Polyline _polyline, _calcPolyline;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly string _unit;
    private readonly string _labels;
    private readonly float _fontSize;

    public CircleArcElement(Polyline polyline,
                            ArgbColor color,
                            float width,
                            string unit = "both",
                            string labels = "all",
                            float fontSize = 11f,
                            Polyline calcPolyline = null)
    {
        _polyline = polyline;
        _color = color;
        _width = width;
        _unit = unit;
        _labels = String.IsNullOrEmpty(labels) ? "all" : labels.ToLowerInvariant();
        _fontSize = fontSize;
        _calcPolyline = calcPolyline;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        using (var helper = new AngleElementHelper(canvas, map, _polyline, _calcPolyline, _color, _width, _fontSize))
        {
            var p = helper.CalcPoints;
            if (p.Count < 2)
            {
                return;
            }

            var arc = p.Count >= 3 ? CircleArcCalculator.Calc(p) : null;
            if (arc is null)
            {
                // less than 3 vertices or collinear: only the line
                helper.DrawLine(p.Count >= 3 ? [p[0], p[1], p[2]] : [p[0], p[1]], false);
                return;
            }

            var c = arc.Center;
            double r = arc.Radius, mid = arc.MidDirection;
            var midPoint = arc.At(r, mid);

            helper.DrawLine(arc.ArcPoints(), false);

            // radius lines, center cross
            helper.DrawLine([c, arc.At(r, arc.Start)], true);
            helper.DrawLine([c, arc.At(r, arc.Start + arc.Sweep)], true);
            helper.DrawLine([c, midPoint], true);
            double cross = r * CrossFactor;
            helper.DrawLine([new Point(c.X - cross, c.Y), new Point(c.X + cross, c.Y)], true);
            helper.DrawLine([new Point(c.X, c.Y - cross), new Point(c.X, c.Y + cross)], true);

            if (_labels == "all" || _labels == "radius")
            {
                helper.DrawParallelLabel(arc.At(r * RadiusLabelFactor, mid), c, midPoint,
                                         $"R = {AngleElementHelper.FormatNumber(r)} m", above: true);
            }
            if (_labels == "all" || _labels == "length")
            {
                // tangent at the arc midpoint, text outside of the arc (the center is below the text)
                var tangentTo = new Point(midPoint.X - Math.Sin(mid), midPoint.Y + Math.Cos(mid));
                helper.DrawParallelLabel(midPoint, midPoint, tangentTo,
                                         $"b = {AngleElementHelper.FormatNumber(arc.Length)} m",
                                         above: !helper.IsAboveText(midPoint, midPoint, tangentTo, c));
            }
            if (_labels == "all" || _labels == "angle")
            {
                helper.DrawLabel(arc.At(r * AngleLabelFactor, mid), $"α = {AngleElementHelper.FormatAngle(arc.AngleDegrees, _unit)}");
            }
        }
    }

    public Envelope Extent =>
        _polyline is not null
            ? new Envelope(_polyline.ShapeEnvelope)
            : null;
}
