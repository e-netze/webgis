using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace E.Standard.WebMapping.Core.Geometry;

// MapMarkup measure tool 'buffer_zone': symmetric buffer around a point, multipoint, line or polygon source
// polygons can also have a negative width (inner buffer)
// input/output coordinates are WGS84 [lng, lat], the buffer is calculated in the calc crs (meters)
// without a (known) calc crs a local equirectangular approximation is used (like L.ViewAngle.toCalc)
static public class BufferZoneCalculator
{
    static public int MinVertexCount(string sourceType) => sourceType switch
    {
        "polygon" => 3,
        "line" => 2,
        _ => 1
    };

    // negative width (inner buffer) only for polygons
    static public bool IsValidWidth(string sourceType, double width)
        => !double.IsNaN(width) && !double.IsInfinity(width) && (sourceType == "polygon" ? width != 0 : width > 0);

    // multipart sources (line, polygon): the vertices are a flat list, partStarts => index of the first vertex of each part
    // (bz-parts: comma separated, eg. "0,5,9"). Polygons: rings inside other rings are holes (even-odd)
    static public int[] ParsePartStarts(string partStarts)
        => String.IsNullOrWhiteSpace(partStarts)
            ? null
            : partStarts.Split(',')
                .Select(s => int.TryParse(s.Trim(), out int i) ? i : -1)
                .Where(i => i >= 0)
                .ToArray();

    // point sources ignore the parts
    static public List<List<Point>> SplitParts(IList<Point> points, int[] partStarts, string sourceType)
    {
        var parts = new List<List<Point>>();
        if (points is null || points.Count == 0)
        {
            return parts;
        }

        var starts = (sourceType == "point" || sourceType == "multipoint" || partStarts is null)
            ? new List<int>()
            : partStarts.Where(s => s > 0 && s < points.Count).Distinct().OrderBy(s => s).ToList();
        starts.Insert(0, 0);

        for (int i = 0; i < starts.Count; i++)
        {
            int end = i + 1 < starts.Count ? starts[i + 1] : points.Count;
            parts.Add(points.Skip(starts[i]).Take(end - starts[i]).ToList());
        }

        return parts;
    }

    static public double[][][] CalcBufferRings(double[][] lngLats, string sourceType, double width, int calcCrsId, int[] partStarts = null)
        => CalcBufferRings(lngLats?
                .Where(c => c != null && c.Length >= 2)
                .Select(c => new Point(c[0], c[1])), sourceType, width, calcCrsId, partStarts);

    static public double[][][] CalcBufferRings(IEnumerable<Point> lngLats, string sourceType, double width, int calcCrsId, int[] partStarts = null)
    {
        var points = lngLats?.Select(p => new Point(p.X, p.Y)).ToList();
        if (points is null || !IsValidWidth(sourceType, width) || points.Count < MinVertexCount(sourceType))
        {
            return null;
        }

        var sRef = calcCrsId > 0 ? CoreApiGlobals.SRefStore?.SpatialReferences?.ById(calcCrsId) : null;
        GeometricTransformerPro transformer = null;
        double kx = 111320.0 * Math.Cos(points[0].Y * Math.PI / 180.0), ky = 110540.0;

        try
        {
            if (sRef != null)
            {
                transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, calcCrsId);
                var shapePoints = new Polyline(new Path(points));
                transformer.Transform(shapePoints);
                points = SpatialAlgorithms.ShapePoints(shapePoints, true);
            }
            else
            {
                points = points.Select(p => new Point(p.X * kx, p.Y * ky)).ToList();
            }

            int minVertexCount = MinVertexCount(sourceType);
            var parts = SplitParts(points, partStarts, sourceType)
                .Where(p => p.Count >= minVertexCount)  // eg. the part currently drawn
                .ToList();
            if (parts.Count == 0)
            {
                return null;
            }

            Shape source = sourceType switch
            {
                "line" => new Polyline(parts.Select(p => new Path(p))),
                "polygon" => new Polygon(parts.Select(p => new Ring(p))),
                "multipoint" => new MultiPoint(points),
                _ => points[0]
            };

            Polygon buffer;
            using (var cts = new CancellationTokenSource())
            {
                buffer = source.CalcBuffer(width, cts);
            }

            if (buffer is null || buffer.RingCount == 0)
            {
                return null;
            }

            if (transformer != null)
            {
                transformer.InvTransform(buffer);
            }

            var rings = new List<double[][]>();
            for (int r = 0; r < buffer.RingCount; r++)
            {
                var ring = buffer[r];
                var coords = new List<double[]>();
                for (int p = 0; p < ring.PointCount; p++)
                {
                    double x = ring[p].X, y = ring[p].Y;
                    if (transformer is null)
                    {
                        x /= kx;
                        y /= ky;
                    }
                    coords.Add([Math.Round(x, 8), Math.Round(y, 8)]);
                }

                if (coords.Count >= 3)
                {
                    rings.Add(coords.ToArray());
                }
            }

            return rings.Count > 0 ? rings.ToArray() : null;
        }
        finally
        {
            transformer?.Dispose();
        }
    }

    static public Polygon ToPolygon(double[][][] rings)
    {
        if (rings is null || rings.Length == 0)
        {
            return null;
        }

        var polygon = new Polygon(rings
            .Where(r => r != null && r.Length >= 3)
            .Select(r => new Ring(r.Where(c => c != null && c.Length >= 2).Select(c => new Point(c[0], c[1])).ToList())));

        return polygon.RingCount > 0 ? polygon : null;
    }
}
