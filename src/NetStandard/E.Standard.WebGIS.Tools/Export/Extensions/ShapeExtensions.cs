#nullable enable

using E.Standard.OGC.Schema.wfs_1_0_0;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

static internal class ShapeExtensions
{
    static public MultiPoint? MultiPointFromPointOrMultiPoint(this Shape shape)
        => shape switch
        {
            MultiPoint multiPoint => multiPoint,
            Point point => new MultiPoint([point]),
            _ => null
        };

    static public Polygon SeriesPagePolygon(this Point centerPoint, double pageWidth, double pageHeight, double angle)
    {
        var halfWidth = pageWidth / 2.0;
        var halfHeight = pageHeight / 2.0;
        var corners = new List<Point>
        {
            new Point(-halfWidth, -halfHeight),
            new Point(halfWidth, -halfHeight),
            new Point(halfWidth, halfHeight),
            new Point(-halfWidth, halfHeight)
        };
        var rotatedCorners = corners.Select(c =>
        {
            var rotatedX = c.X * Math.Cos(angle) - c.Y * Math.Sin(angle);
            var rotatedY = c.X * Math.Sin(angle) + c.Y * Math.Cos(angle);
            return new Point(rotatedX + centerPoint.X, rotatedY + centerPoint.Y);
        }).ToArray();
        var polygon = new Polygon();
        var ring = new Ring(new PointCollection(rotatedCorners));

        polygon.AddRing(ring);

        return polygon;
    }

    static public Shape ReducePointNummerTo(this Shape shape, int count)
    {
        if (shape.CountPoints() < count) return shape;

        return shape switch
        {
            MultiPoint multiPoint => new MultiPoint(shape.AllPoints().Take(count)),
            _ => throw new NotImplementedException($"ReducePointNumberTo is not implemented for {shape.GetType().Name}")
        };
    }

    static public IEnumerable<Polyline> ExplodeToSinglePathPolylines(this Polyline polyline)
    {
        if (polyline == null) return [];
        if (polyline.PathCount == 1) return [polyline];

        return polyline.ToArray()
            .Select(path => new Polyline(path))
            .ToArray();
    }

    static public IEnumerable<PointM> PolygonPointsWithStat(this Polyline polyline)
    {
        if (polyline.PathCount > 1)
        {
            throw new ArgumentException("Only single part polylines allowed");
        }

        var pathPoints = polyline.AllPoints().ToArray();
        double stat = 0D;

        List<PointM> pointsWithStat = new List<PointM>();
        pointsWithStat.Add(new PointM(pathPoints[0].X, pathPoints[0].Y, 0, stat));

        for (int i = 1; i < pathPoints.Length; i++)
        {
            stat += Math.Sqrt(
               (pathPoints[i].X - pathPoints[i - 1].X) * (pathPoints[i].X - pathPoints[i - 1].X) +
               (pathPoints[i].Y - pathPoints[i - 1].Y) * (pathPoints[i].Y - pathPoints[i - 1].Y));

            pointsWithStat.Add(new PointM(pathPoints[i].X, pathPoints[i].Y, 0, stat));
        }

        return pointsWithStat;

        // todo: make this faster

        //return polyline.AllPoints().Select(p =>
        //{
        //    p = SpatialAlgorithms.Point2PolylineDistance(polyline, p, out double dist, out double stat);
        //    return new PointM(p.X, p.Y, 0, stat);
        //})
        //.ToArray();
    }

    static public IEnumerable<PointM> PointsMBetween(this IEnumerable<PointM> points, double statFrom, double statTo)
    {
        return points
            .Select(p => (p, Convert.ToDouble(p.M)))
            .Where(o => o.Item2 >= statFrom && o.Item2 <= statTo)
            .OrderBy(o => o.Item2)
            .Select(o => o.Item1)
            .ToArray();
    }

}
