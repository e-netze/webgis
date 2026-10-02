using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core.Tests;

public class ShapeMetricsTests
{
    [Theory]
    [InlineData("shape_area", 100d)]
    [InlineData("SHAPE_PERIMETER", 40d)]
    [InlineData("shape_centroid_x", 5d)]
    [InlineData("shape_minx", 0d)]
    [InlineData("shape_maxy", 10d)]
    public void GetNumericMetric_Polygon_ReturnsValue(string name, double expected)
    {
        Assert.Equal(expected, ShapeMetrics.GetNumericMetric(name, CreateSquare()));
    }

    [Fact]
    public void GetNumericMetric_ShapeTypeMismatchOrUnknown_ReturnsNull()
    {
        Assert.Null(ShapeMetrics.GetNumericMetric("shape_len", CreateSquare()));
        Assert.Null(ShapeMetrics.GetNumericMetric("shape_unknown", CreateSquare()));
        Assert.Null(ShapeMetrics.GetNumericMetric("shape_area", null));
        Assert.False(ShapeMetrics.IsNumericMetric("shape_type"));
    }

    [Fact]
    public void GetTypeName_ReturnsLowercaseName()
    {
        Assert.Equal("polygon", ShapeMetrics.GetTypeName(CreateSquare()));
        Assert.Equal("point", ShapeMetrics.GetTypeName(new Point(1, 2)));
        Assert.Null(ShapeMetrics.GetTypeName(null));
    }

    [Fact]
    public void TransformedCopy_LeavesSourceUnchanged()
    {
        var source = new Point(1, 2) { SrsId = 1 };

        var copy = source.TransformedCopy(2, (_, _) => new OffsetTransformer());

        Assert.NotSame(source, copy);
        Assert.Equal(1, source.X);
        Assert.Equal(1, source.SrsId);
        Assert.Equal(11, ((Point)copy).X);
        Assert.Equal(22, ((Point)copy).Y);
    }

    [Fact]
    public void TransformedCopy_SameSRefId_ReturnsSourceWithoutTransformer()
    {
        var source = new Point(1, 2) { SrsId = 1 };

        var copy = source.TransformedCopy(
            1,
            (_, _) => throw new InvalidOperationException());

        Assert.Same(source, copy);
    }

    private static Polygon CreateSquare()
        => new(new Ring(
        [
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10),
            new Point(0, 10),
            new Point(0, 0)
        ]));

    private sealed class OffsetTransformer : IGeometricTransformer
    {
        public void Transform(double[] x, double[] y) { }

        public void Transform(Shape shape)
        {
            var point = (Point)shape;
            point.X += 10;
            point.Y += 20;
        }

        public void InvTransform(Shape shape) { }

        public void Dispose() { }
    }
}
