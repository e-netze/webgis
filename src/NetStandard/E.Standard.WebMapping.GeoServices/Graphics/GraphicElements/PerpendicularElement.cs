using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;

using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;

// MapMarkup measure tool 'perpendicular' (Normalabstand)
// the first lineVertexCount vertices are the reference line, all other vertices are points
// for each point the perpendicular to the reference line is drawn and labeled with its length (optional with the station of the foot point)
// same geometry as webgis-graphics.perpendicular.js
public class PerpendicularElement : IGraphicElement
{
    private readonly Polyline _polyline, _calcPolyline;
    private readonly ArgbColor _color;
    private readonly float _width;
    private readonly int _lineVertexCount;
    private readonly string _stationFormat;
    private readonly Unit _lengthUnit;
    private readonly float _fontSize;

    public PerpendicularElement(Polyline polyline,
                                ArgbColor color,
                                float width,
                                int lineVertexCount,
                                string stationFormat = "m",
                                string lengthUnit = "m",
                                float fontSize = 11f,
                                Polyline calcPolyline = null)
    {
        _polyline = polyline;
        _color = color;
        _width = width;
        _lineVertexCount = lineVertexCount;
        _stationFormat = !String.IsNullOrEmpty(stationFormat) ? stationFormat : "m";
        _lengthUnit = (!String.IsNullOrEmpty(lengthUnit) ? lengthUnit : "m").FromUnitAbbreviation();
        _fontSize = fontSize;
        _calcPolyline = calcPolyline;
    }

    public void Draw(ICanvas canvas, IMap map)
    {
        using (var helper = new AngleElementHelper(canvas, map, _polyline, _calcPolyline, _color, _width, _fontSize))
        {
            var points = helper.CalcPoints;
            int lineCount = _lineVertexCount > 0 ? Math.Min(_lineVertexCount, points.Count) : points.Count;

            var linePoints = points.Take(lineCount).ToList();
            if (linePoints.Count >= 2)
            {
                helper.DrawLine(linePoints, false);
            }

            for (int p = lineCount; p < points.Count; p++)
            {
                var point = points[p];
                helper.FillPoint(point, Math.Max(3f, _width + 1f));

                var foot = Foot(linePoints, point);
                if (foot is null)
                {
                    continue;
                }

                var footPoint = new Point(foot.X, foot.Y);
                if (foot.Extension is not null)
                {
                    helper.DrawLine([foot.Extension, footPoint], true);
                }

                if (foot.Dist > 0)
                {
                    helper.DrawLine([point, footPoint], false);

                    double s = Math.Min(foot.Dist, foot.SegLength) * 0.1,
                           vx = (point.X - foot.X) / foot.Dist, vy = (point.Y - foot.Y) / foot.Dist;
                    helper.DrawLine([
                        new Point(foot.X + foot.Ux * s, foot.Y + foot.Uy * s),
                        new Point(foot.X + foot.Ux * s + vx * s, foot.Y + foot.Uy * s + vy * s),
                        new Point(foot.X + vx * s, foot.Y + vy * s)], false, Math.Max(1f, _width / 2f));

                    helper.DrawSegmentLength(footPoint, point, foot.Dist, _lengthUnit);
                }

                var stationText = FormatStation(foot.Station, _stationFormat);
                if (stationText is not null)
                {
                    var dirTo = new Point(foot.X + foot.Ux, foot.Y + foot.Uy);
                    helper.DrawParallelLabel(footPoint, footPoint, dirTo, stationText,
                                             above: !helper.IsAboveText(footPoint, footPoint, dirTo, point));
                }
            }
        }
    }

    #region Algorithm (same as L.Perpendicular.foot)

    public class FootResult
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Dist { get; set; }
        public double Station { get; set; }
        public double Ux { get; set; }
        public double Uy { get; set; }
        public double SegLength { get; set; }
        public Point Extension { get; set; }
    }

    // the first/last segment is extended, if the foot is before the start/after the end of the line
    static public FootResult Foot(List<Point> linePoints, Point point)
    {
        if (linePoints is null || linePoints.Count < 2)
        {
            return null;
        }

        FootResult best = null;
        double station = 0;
        int last = linePoints.Count - 2;

        for (int i = 0; i <= last; i++)
        {
            Point a = linePoints[i], b = linePoints[i + 1];
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 0)
            {
                double t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (len * len);
                if ((t >= 0 && t <= 1) || (i == 0 && t < 0) || (i == last && t > 1))
                {
                    double x = a.X + t * dx, y = a.Y + t * dy;
                    double dist = Math.Sqrt((point.X - x) * (point.X - x) + (point.Y - y) * (point.Y - y));
                    if (best is null || dist < best.Dist)
                    {
                        best = new FootResult()
                        {
                            X = x,
                            Y = y,
                            Dist = dist,
                            Station = station + t * len,
                            Ux = dx / len,
                            Uy = dy / len,
                            SegLength = len,
                            Extension = t < 0 ? new Point(a.X, a.Y) : (t > 1 ? new Point(b.X, b.Y) : null)
                        };
                    }
                }
            }
            station += len;
        }

        // no valid foot on a segment (point is "behind" a vertex) => nearest vertex
        station = 0;
        for (int v = 0; v < linePoints.Count; v++)
        {
            if (v > 0)
            {
                double sx = linePoints[v].X - linePoints[v - 1].X, sy = linePoints[v].Y - linePoints[v - 1].Y;
                station += Math.Sqrt(sx * sx + sy * sy);
            }

            var vertex = linePoints[v];
            double vDist = Math.Sqrt((point.X - vertex.X) * (point.X - vertex.X) + (point.Y - vertex.Y) * (point.Y - vertex.Y));
            if (best is null || vDist < best.Dist - 1e-9)
            {
                Point n = linePoints[Math.Min(v + 1, linePoints.Count - 1)], m = linePoints[Math.Max(v - 1, 0)];
                double ddx = n.X - m.X, ddy = n.Y - m.Y, dlen = Math.Sqrt(ddx * ddx + ddy * ddy);
                if (dlen == 0)
                {
                    dlen = 1;
                }

                best = new FootResult()
                {
                    X = vertex.X,
                    Y = vertex.Y,
                    Dist = vDist,
                    Station = station,
                    Ux = ddx / dlen,
                    Uy = ddy / dlen,
                    SegLength = dlen,
                    Extension = null
                };
            }
        }

        return best;
    }

    // format: none => null, m => 145,20m, km => 0+145,20 (km+m); negative before the start of the line
    static public string FormatStation(double meters, string format)
    {
        switch (format?.ToLowerInvariant())
        {
            case "none":
                return null;
            case "km":
                break;
            default:
                return $"{AngleElementHelper.FormatNumber(meters)}m";
        }

        double value = Math.Round(Math.Abs(meters) * 100.0) / 100.0;
        double km = Math.Floor(value / 1000.0), m = value - km * 1000.0;

        return $"{(meters < 0 && value > 0 ? "-" : "")}{km.ToString("0", CultureInfo.InvariantCulture)}+{m.ToString("000.00", CultureInfo.CurrentCulture)}";
    }

    #endregion

    public Envelope Extent =>
        _polyline is not null
            ? new Envelope(_polyline.ShapeEnvelope)
            : null;
}
