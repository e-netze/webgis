using System;
using System.Collections.Generic;
using System.Linq;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// MapMarkup measure tool 'buffer_zone' (Pufferzone)
// polyline: source vertices (sourceType point => 1 vertex, multipoint => points, line, polygon => rings without closing vertex)
// partStarts: multipart line/polygon (flat vertex list, index of the first vertex of each part)
// buffer: the buffer polygon (map srs), calculated by BufferZoneCalculator (polygons: negative width => inner buffer)
// the buffer width is labeled with a dimension line, same geometry as webgis-graphics.buffer_zone.js (L.BufferZone.dimension)
public class BufferZoneElement : IGraphicElement
{
    private readonly Polyline _polyline, _calcPolyline;
    private readonly Polygon _buffer;
    private readonly ArgbColor _color, _fillColor;
    private readonly float _width;
    private readonly string _sourceType;
    private readonly double _bufferWidth;
    private readonly float _fontSize;
    private readonly int[] _partStarts;

    public BufferZoneElement(Polyline polyline,
                             Polygon buffer,
                             ArgbColor color,
                             ArgbColor fillColor,
                             float width,
                             string sourceType,
                             double bufferWidth,
                             int[] partStarts = null,
                             float fontSize = 11f,
                             Polyline calcPolyline = null)
    {
        _polyline = polyline;
        _buffer = buffer;
        _color = color;
        _fillColor = fillColor;
        _width = width;
        _sourceType = !String.IsNullOrEmpty(sourceType) ? sourceType.ToLowerInvariant() : "line";
        _bufferWidth = bufferWidth;
        _partStarts = partStarts;
        _fontSize = fontSize;
        _calcPolyline = calcPolyline;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        DrawBuffer(canvas, map);

        using (var helper = new AngleElementHelper(canvas, map, _polyline, _calcPolyline, _color, _width, _fontSize))
        {
            var points = helper.CalcPoints;
            if (points.Count == 0)
            {
                return;
            }

            bool pointSource = _sourceType == "point" || _sourceType == "multipoint";
            var parts = BufferZoneCalculator.SplitParts(points, _partStarts, _sourceType);

            if (pointSource || points.Count < 2)
            {
                foreach (var point in _sourceType == "multipoint" ? points : points.Take(1))
                {
                    helper.FillPoint(point, Math.Max(3f, _width + 1f));
                }
            }
            else
            {
                foreach (var part in parts)
                {
                    if (_sourceType == "polygon" && part.Count > 2)
                    {
                        helper.DrawLine(part.Concat([part[0]]), false);
                    }
                    else if (part.Count > 1)
                    {
                        helper.DrawLine(part, false);
                    }
                }
            }

            if (_buffer is not null && BufferZoneCalculator.IsValidWidth(_sourceType, _bufferWidth))
            {
                // multipart: only at the first part
                var dim = Dimension(pointSource ? points.Take(1).ToList() : parts[0], _sourceType, _bufferWidth);
                if (dim is not null)
                {
                    helper.DrawLine([dim.Value.from, dim.Value.to], false, Math.Max(1f, _width / 2f));
                    helper.DrawSegmentLength(dim.Value.from, dim.Value.to, _bufferWidth, "m".FromUnitAbbreviation());
                }
            }
        }
    }

    private void DrawBuffer(ICanvas canvas, IMap map)
    {
        if (_buffer is null || _buffer.RingCount == 0)
        {
            return;
        }

        try
        {
            using (var pen = Current.Engine.CreatePen(_color, 1f * map.DpiFactor()))
            using (var brush = Current.Engine.CreateSolidBrush(_fillColor))
            using (var gp = Current.Engine.CreateGraphicsPath())
            {
                for (int i = 0; i < _buffer.RingCount; i++)
                {
                    Ring ring = _buffer[i];
                    ring.Close();

                    gp.StartFigure();
                    Point last = null;
                    for (int p = 0; p < ring.PointCount; p++)
                    {
                        Point point = map.WorldToImage(ring[p]);
                        if (last is not null)
                        {
                            gp.AddLine((float)last.X, (float)last.Y, (float)point.X, (float)point.Y);
                        }
                        last = point;
                    }
                }

                canvas.FillPath(brush, gp);
                canvas.DrawPath(pen, gp);
            }
        }
        catch { }
    }

    #region Algorithm (same as L.BufferZone.dimension)

    // point/multipoint (or < 2 points): from the (first) point to the east
    // line/polygon: perpendicular to the first segment from its midpoint, for polygons outside (negative width => inside)
    static public (Point from, Point to)? Dimension(List<Point> points, string sourceType, double width)
    {
        if (points is null || points.Count == 0 || width == 0 || double.IsNaN(width))
        {
            return null;
        }

        Point from = points[0];
        double nx = 1, ny = 0;

        if (sourceType != "point" && sourceType != "multipoint" && points.Count >= 2)
        {
            Point a = points[0], b = points[1];
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 0)
            {
                nx = -dy / len; ny = dx / len;
                from = new Point((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5);

                if (sourceType == "polygon" && points.Count >= 3)
                {
                    double eps = len * 1e-3;
                    if (PointInPolygon(points, from.X + nx * eps, from.Y + ny * eps))
                    {
                        nx = -nx; ny = -ny;
                    }
                }
            }
        }

        return (new Point(from.X, from.Y), new Point(from.X + nx * width, from.Y + ny * width));
    }

    // ray casting
    static private bool PointInPolygon(List<Point> points, double x, double y)
    {
        bool inside = false;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            double xi = points[i].X, yi = points[i].Y, xj = points[j].X, yj = points[j].Y;
            if (((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    #endregion

    public Envelope Extent =>
        _buffer is not null
            ? new Envelope(_buffer.ShapeEnvelope)
            : _polyline is not null
                ? new Envelope(_polyline.ShapeEnvelope)
                : null;
}
