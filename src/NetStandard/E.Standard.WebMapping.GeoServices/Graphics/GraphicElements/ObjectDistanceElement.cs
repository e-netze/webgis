using System;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// MapMarkup measure tool 'object_distance' (Objektabstand)
// each path (2 points) is the shortest connection from object A to an object B
// drawn like a dimline (end markers) and labeled with its length (optional with its number), touching objects (length 0) => no line
// same geometry as webgis-graphics.object_distance.js
public class ObjectDistanceElement : IGraphicElement
{
    private readonly Polyline _polyline, _calcPolyline;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly Unit _lengthUnit;
    private readonly bool _showNumber;
    private readonly float _fontSize;

    public ObjectDistanceElement(Polyline polyline,
                                 ArgbColor color,
                                 float width,
                                 string lengthUnit = "m",
                                 bool showNumber = false,
                                 float fontSize = 11f,
                                 Polyline calcPolyline = null)
    {
        _polyline = polyline;
        _color = color;
        _width = width;
        _lengthUnit = (!String.IsNullOrEmpty(lengthUnit) ? lengthUnit : "m").FromUnitAbbreviation();
        _showNumber = showNumber;
        _fontSize = fontSize;
        _calcPolyline = calcPolyline;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        using (var helper = new AngleElementHelper(canvas, map, _polyline, _calcPolyline, _color, _width, _fontSize))
        {
            var points = helper.CalcPoints;

            for (int i = 0; i + 1 < points.Count; i += 2)
            {
                Point from = points[i], to = points[i + 1];
                double dx = to.X - from.X, dy = to.Y - from.Y, length = Math.Sqrt(dx * dx + dy * dy);
                if (length == 0)
                {
                    continue;
                }

                helper.DrawLine([from, to], false);
                helper.FillPoint(from, Math.Max(3f, _width + 1f));
                helper.FillPoint(to, Math.Max(3f, _width + 1f));

                var text = $"{AngleElementHelper.FormatNumber(length.MetersToUnit(_lengthUnit))}{_lengthUnit.ToAbbreviation()}";
                if (_showNumber)
                {
                    text = $"{i / 2 + 1}: {text}";
                }

                var mid = new Point((from.X + to.X) * 0.5, (from.Y + to.Y) * 0.5);
                helper.DrawParallelLabel(mid, from, to, text, above: true);
            }
        }
    }

    public Envelope Extent =>
        _polyline is not null
            ? new Envelope(_polyline.ShapeEnvelope)
            : null;
}
