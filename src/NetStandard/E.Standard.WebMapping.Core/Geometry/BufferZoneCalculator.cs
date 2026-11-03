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

    static public double[][][] CalcBufferRings(double[][] lngLats, string sourceType, double width, int calcCrsId)
        => CalcBufferRings(lngLats?
                .Where(c => c != null && c.Length >= 2)
                .Select(c => new Point(c[0], c[1])), sourceType, width, calcCrsId);

    static public double[][][] CalcBufferRings(IEnumerable<Point> lngLats, string sourceType, double width, int calcCrsId)
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

            Shape source = sourceType switch
            {
                "line" => new Polyline(new Path(points)),
                "polygon" => new Polygon(new Ring(points)),
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
