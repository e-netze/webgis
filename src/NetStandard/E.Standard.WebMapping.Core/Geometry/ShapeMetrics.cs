using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Geometry;

public static class ShapeMetrics
{
    private static readonly IReadOnlyDictionary<string, Func<Shape, double?>> NumericMetrics =
        new Dictionary<string, Func<Shape, double?>>(StringComparer.OrdinalIgnoreCase)
        {
            ["shape_len"] = shape => (shape as Polyline)?.Length,
            ["shape_area"] = shape => (shape as Polygon)?.Area,
            ["shape_perimeter"] = shape => (shape as Polygon)?.Circumference,
            ["shape_centroid_x"] = shape => SpatialAlgorithms.Centroid(shape)?.X,
            ["shape_centroid_y"] = shape => SpatialAlgorithms.Centroid(shape)?.Y,
            ["shape_minx"] = shape => shape.ShapeEnvelope?.MinX,
            ["shape_miny"] = shape => shape.ShapeEnvelope?.MinY,
            ["shape_maxx"] = shape => shape.ShapeEnvelope?.MaxX,
            ["shape_maxy"] = shape => shape.ShapeEnvelope?.MaxY
        };

    /// <summary>
    /// Coordinate-dependent metrics; these can be calculated in a target SRefId.
    /// </summary>
    public static bool IsNumericMetric(string name)
        => name is not null && NumericMetrics.ContainsKey(name);

    public static double? GetNumericMetric(string name, Shape shape)
        => shape is not null && NumericMetrics.TryGetValue(name, out var metric)
            ? metric(shape)
            : null;

    public static string GetTypeName(Shape shape)
        => shape switch
        {
            null => null,
            Point => "point",
            MultiPoint => "multipoint",
            Polyline => "polyline",
            Polygon => "polygon",
            Envelope => "envelope",
            _ => shape.GetType().Name.ToLowerInvariant()
        };
}
