using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core.Tests;

public class SpatialAlgorithmsTests
{
    [Fact]
    public void Centroid_Point_ReturnsSamePoint()
    {
        var point = new Point(7, 9);

        var centroid = SpatialAlgorithms.Centroid(point);

        Assert.Same(point, centroid);
    }

    [Fact]
    public void Centroid_MultiPoint_ReturnsCoordinateMean()
    {
        var multiPoint = new MultiPoint(
        [
            new Point(0, 0),
            new Point(10, 20)
        ]);

        var centroid = SpatialAlgorithms.Centroid(multiPoint);

        Assert.Equal(5, centroid.X);
        Assert.Equal(10, centroid.Y);
    }

    [Fact]
    public void Centroid_Polyline_ReturnsPointAtHalfLength()
    {
        var polyline = new Polyline(
        [
            new Point(0, 0),
            new Point(10, 0),
            new Point(10, 10)
        ]);

        var centroid = SpatialAlgorithms.Centroid(polyline);

        Assert.Equal(10, centroid.X);
        Assert.Equal(0, centroid.Y);
    }

    [Fact]
    public void Centroid_PolygonWithHole_SubtractsHoleArea()
    {
        var polygon = CreatePolygonWithHole();

        var centroid = SpatialAlgorithms.Centroid(polygon);

        Assert.Equal(4.916666666666667, centroid.X, precision: 12);
        Assert.Equal(4.916666666666667, centroid.Y, precision: 12);
    }

    [Fact]
    public void VertexAndPartCount_PolygonWithHole_CountVerticesButNotHoleAsPart()
    {
        var polygon = CreatePolygonWithHole();

        Assert.Equal(10, SpatialAlgorithms.VertexCount(polygon));
        Assert.Equal(1, SpatialAlgorithms.PartCount(polygon));
    }

    private static Polygon CreatePolygonWithHole()
        => new(
        [
            new Ring(
            [
                new Point(0, 0),
                new Point(10, 0),
                new Point(10, 10),
                new Point(0, 10),
                new Point(0, 0)
            ]),
            new Ring(
            [
                new Point(6, 6),
                new Point(8, 6),
                new Point(8, 8),
                new Point(6, 8),
                new Point(6, 6)
            ])
        ]);
}
