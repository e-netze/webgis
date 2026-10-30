using System;
using System.Collections.Generic;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// MapMarkup measure tool 'intersection_angle' (Schnittwinkel): vertices 0,1 => line 1, vertices 2,3 => line 2
// same geometry as webgis-graphics.intersection_angle.js
public class IntersectionAngleElement : IGraphicElement
{
    private readonly Polyline _polyline, _calcPolyline;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly string _unit;
    private readonly string _angles;
    private readonly float _fontSize;

    public IntersectionAngleElement(Polyline polyline,
                                    ArgbColor color,
                                    float width,
                                    string unit = "both",
                                    string angles = "angle",
                                    float fontSize = 11f,
                                    Polyline calcPolyline = null)
    {
        _polyline = polyline;
        _color = color;
        _width = width;
        _unit = unit;
        _angles = angles;
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

            helper.DrawLine([p[0], p[1]], false);
            if (p.Count < 4)
            {
                return;
            }
            helper.DrawLine([p[2], p[3]], false);

            double d1X = p[1].X - p[0].X, d1Y = p[1].Y - p[0].Y,
                   d2X = p[3].X - p[2].X, d2Y = p[3].Y - p[2].Y;
            double len1 = Math.Sqrt(d1X * d1X + d1Y * d1Y), len2 = Math.Sqrt(d2X * d2X + d2Y * d2Y);
            if (len1 == 0 || len2 == 0)
            {
                return;
            }

            double cross = d1X * d2Y - d1Y * d2X;
            if (Math.Abs(cross) / (len1 * len2) < 1e-9)
            {
                helper.DrawLabel(new Point((p[0].X + p[1].X + p[2].X + p[3].X) / 4.0, (p[0].Y + p[1].Y + p[2].Y + p[3].Y) / 4.0), "parallel");
                return;
            }

            double wX = p[2].X - p[0].X, wY = p[2].Y - p[0].Y;
            double t = (wX * d2Y - wY * d2X) / cross, u = (wX * d1Y - wY * d1X) / cross;
            var s = new Point(p[0].X + t * d1X, p[0].Y + t * d1Y);

            // extensions
            if (t < 0) helper.DrawLine([s, p[0]], true);
            else if (t > 1) helper.DrawLine([p[1], s], true);
            if (u < 0) helper.DrawLine([s, p[2]], true);
            else if (u > 1) helper.DrawLine([p[3], s], true);

            double Dist(Point pt) => Math.Sqrt((pt.X - s.X) * (pt.X - s.X) + (pt.Y - s.Y) * (pt.Y - s.Y));
            double radius = 0.3 * Math.Min(Math.Max(Dist(p[0]), Dist(p[1])), Math.Max(Dist(p[2]), Dist(p[3])));

            double a1 = Math.Atan2(d1Y, d1X), a2 = Math.Atan2(d2Y, d2X);
            double Normalize(double delta)
            {
                while (delta > Math.PI) delta -= 2.0 * Math.PI;
                while (delta <= -Math.PI) delta += 2.0 * Math.PI;
                return delta;
            }
            double deltaAngle = Normalize(a2 - a1), deltaAdjacent = Normalize(a2 + Math.PI - a1);

            foreach (var (start, delta) in Sectors(_angles, a1, deltaAngle, deltaAdjacent))
            {
                if (radius > 0)
                {
                    helper.DrawArc(s, radius, start, start + delta);
                }
                helper.DrawLabel(AngleElementHelper.At(s, radius > 0 ? radius * 1.4 : len1 * 0.1, start + delta / 2.0),
                                 AngleElementHelper.FormatAngle(Math.Abs(delta) * 180.0 / Math.PI, _unit));
            }
        }
    }

    // angle: a1 => a2, adjacent: a1 => a2 + 180°, all: additionally mirrored at the intersection (vertical angles)
    private static IEnumerable<(double start, double delta)> Sectors(string angles, double a1, double deltaAngle, double deltaAdjacent)
    {
        angles = angles?.ToLowerInvariant();

        if (angles != "adjacent")
        {
            yield return (a1, deltaAngle);
        }
        if (angles == "adjacent" || angles == "both" || angles == "all")
        {
            yield return (a1, deltaAdjacent);
        }
        if (angles == "all")
        {
            yield return (a1 + Math.PI, deltaAngle);
            yield return (a1 + Math.PI, deltaAdjacent);
        }
    }

    public Envelope Extent =>
        _polyline is not null
            ? new Envelope(_polyline.ShapeEnvelope)
            : null;
}
