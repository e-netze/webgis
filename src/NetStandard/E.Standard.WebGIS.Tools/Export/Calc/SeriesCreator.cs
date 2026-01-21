using E.Standard.WebGIS.Tools.Export.Extensions;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Geometry.Extensions;
using MathNet.Numerics;
using System;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Export.Calc;

internal enum SeriesType
{
    BoundingBoxRaster = 0,
    IntersectionRaster = 1,
    AlongPolylines = 2
}

internal class SeriesCreator
{
    private readonly FeatureCollection _features;
    private readonly double _pageWidth;
    private readonly double _pageHeight;
    private readonly double _overlappingPercent;

    public SeriesCreator(
        FeatureCollection features,
        double pageWidth,
        double pageHeight,
        double overlappingPercent)
    {
        if (pageWidth <= 0) throw new ArgumentException(nameof(pageWidth));
        if(pageHeight <= 0) throw new ArgumentException(nameof(pageHeight));
        if(overlappingPercent < 0 || overlappingPercent > 100) throw new ArgumentException(nameof(overlappingPercent));

        _features = features;
        _pageWidth = pageWidth;
        _pageHeight = pageHeight;
        _overlappingPercent = overlappingPercent;
    }

    public MultiPoint BoundingBoxRaster()
    {
        var featuresBBox = FeatureCollectionBBox(_features);

        double overlapX = _pageWidth * (_overlappingPercent / 100);
        double overlapY = _pageHeight * (_overlappingPercent / 100);
        double stepX = _pageWidth - overlapX;
        double stepY = _pageHeight - overlapY;

        int columns = (int)System.Math.Ceiling((featuresBBox.Width - overlapX) / stepX);
        int rows = (int)System.Math.Ceiling((featuresBBox.Height - overlapY) / stepY);

        double rasterWidth = _pageWidth * columns - overlapX * (columns - 1);
        double rasterHeight = _pageHeight * rows - overlapY * (rows - 1);

        var startX = featuresBBox.CenterPoint.X - rasterWidth / 2;
        var startY = featuresBBox.CenterPoint.Y - rasterHeight / 2;

        var points = new MultiPoint();

        for (int row = rows - 1; row >= 0; row--)
        {
            for (int col = 0; col < columns; col++)
            {
                double x = startX + col * stepX;
                double y = startY + row * stepY;
                points.AddPoint(new PointM(x + _pageWidth / 2, y + _pageHeight / 2, 0, 0));
            }
        }

        return points;
    }

    public MultiPoint IntersectionRaster()
    {
        var raster = BoundingBoxRaster();
        var intersectRaster = new MultiPoint();

        foreach (var p in raster.ToArray())
        {
            var pageBox = new Envelope(
                        p.X - _pageWidth / 2.0, p.Y - _pageHeight / 2.0,
                        p.X + _pageWidth / 2.0, p.Y + _pageHeight / 2.0);

            if (_features.Any(f => Intersects(pageBox, f.Shape)))
            {
                intersectRaster.AddPoint(p);
            }
        }

        return intersectRaster;
    }

    public MultiPoint SeriesAlongPolylines()
    {
        MultiPoint series = new MultiPoint();

        foreach (var feature in _features)
        {
            if (feature.Shape is Polyline polyline)
            {
                foreach (var singlePathPolyline in polyline.ExplodeToSinglePathPolylines())
                {
                    // 2 * _overlappingPercent to consider overlapping on both sides of the page
                    var lineSeries = SeriesAlongPolyline(singlePathPolyline, _pageWidth * (100 - 2D * _overlappingPercent) / 100D);
                    series.AddPoints(lineSeries.ToArray());
                }
            }
        }

        return series;
    }



    #region Helpers

    private Envelope FeatureCollectionBBox(FeatureCollection features)
    {
        double? minX = null;
        double? minY = null;
        double? maxX = null;
        double? maxY = null;

        foreach (var feature in features)
        {
            var env = feature.Shape.ShapeEnvelope;
            if (minX == null || env.MinX < minX) minX = env.MinX;
            if (minY == null || env.MinY < minY) minY = env.MinY;
            if (maxX == null || env.MaxX > maxX) maxX = env.MaxX;
            if (maxY == null || env.MaxY > maxY) maxY = env.MaxY;
        }
        return new Envelope(minX ?? 0, minY ?? 0, maxX ?? 0, maxY ?? 0);
    }

    private bool Intersects(Shape clipper, Shape clippee)
        => Clip.PerformClip(clipper, clippee) switch
        {
            Shape shape => shape.CountPoints() > 0,
            _ => false,
        };

    private MultiPoint SeriesAlongPolyline(Polyline polyline, double spacing)
    {
        if (spacing <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing));
        }

        if(polyline.PathCount > 1)
        {
            throw new ArgumentException("Only single part polylines allowed");
        }

        var series = new MultiPoint();
        var polylineVerticies = polyline.PolygonPointsWithStat();
        double currentDistance = 0;
        Point currentPoint = polylineVerticies.First();
        double lineLength = polyline.Length;

        while (true)
        {
            double stepWidth = spacing;
            Point point = null;

            while (true)
            {
                point =
                    currentDistance + stepWidth >= lineLength
                    ? polylineVerticies.Last()
                    : SpatialAlgorithms.PolylinePoint(polyline, currentDistance + stepWidth);

                double dx = point.X - currentPoint.X;
                double dy = point.Y - currentPoint.Y;
                double segmentLength = System.Math.Sqrt(dx * dx + dy * dy);
                double angle = System.Math.Atan2(dy / segmentLength, dx / segmentLength);

                var pageCenter = new Point(currentPoint.X + dx / 2.0, currentPoint.Y + dy / 2.0, 0);
                var pagePoints = polylineVerticies.PointsMBetween(currentDistance, currentDistance + stepWidth);
                var pagePolygon = pageCenter.SeriesPagePolygon(_pageWidth, _pageHeight, angle);

                if (!pagePoints.All(p => SpatialAlgorithms.Jordan(pagePolygon, p.X, p.Y)))
                {
                    stepWidth -= _pageWidth * 0.05;

                    if (stepWidth < _pageWidth * 0.1)
                    {
                        throw new Exception("Mission impossible!");
                    }

                    continue;
                }

                series.AddPoint(new PointM(
                    pageCenter.X, pageCenter.Y, 0,
                    angle * 180.0 / Math.PI));

                break;
            }

            if (currentDistance + stepWidth >= lineLength || point is null)
            {
                break;
            }

            currentDistance += stepWidth;
            currentPoint = point;
        }

        return series;
    }

    #endregion
}
