using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;

using GeoPath = E.Standard.WebMapping.Core.Geometry.Path;

namespace E.Standard.WebMapping.Core.Tests;

public class WKTExtensionsTests
{
    private static readonly System.Globalization.NumberFormatInfo _nhi =
        new System.Globalization.CultureInfo("en-US", false).NumberFormat;

    #region Point

    [Fact]
    public void ShapeFromWKT_Point_ReturnsPoint()
    {
        var shape = "POINT(15 47)".ShapeFromWKT();

        var point = Assert.IsType<Point>(shape);
        Assert.Equal(15, point.X);
        Assert.Equal(47, point.Y);
    }

    [Fact]
    public void ShapeFromWKT_Point_WithZ_ReturnsPointWithZ()
    {
        var shape = "POINT(15 47 z:350)".ShapeFromWKT();

        var point = Assert.IsType<Point>(shape);
        Assert.Equal(15, point.X);
        Assert.Equal(47, point.Y);
        Assert.Equal(350, point.Z);
    }

    [Fact]
    public void ShapeFromWKT_Point_WithZAndM_ReturnsPointM()
    {
        var shape = "POINT(15 47 z:350 m:21.1)".ShapeFromWKT();

        var point = Assert.IsType<PointM>(shape);
        Assert.Equal(15, point.X);
        Assert.Equal(47, point.Y);
        Assert.Equal(350, point.Z);
        Assert.Equal(21.1, Convert.ToDouble(point.M, _nhi));
    }

    [Fact]
    public void ShapeFromWKT_Point_WithZAndM2_ReturnsPointM2()
    {
        var shape = "POINT(15 47 z:350 m:21.1 m2:5.5)".ShapeFromWKT();

        var point = Assert.IsType<PointM2>(shape);
        Assert.Equal(15, point.X);
        Assert.Equal(47, point.Y);
        Assert.Equal(350, point.Z);
        Assert.Equal(21.1, Convert.ToDouble(point.M, _nhi));
        Assert.Equal(5.5, Convert.ToDouble(point.M2, _nhi));
    }

    [Fact]
    public void WKTFromShape_Point_RoundTrip()
    {
        var original = new Point(15, 47);
        var wkt = original.WKTFromShape();
        var restored = wkt.ShapeFromWKT();

        var point = Assert.IsType<Point>(restored);
        Assert.Equal(original.X, point.X);
        Assert.Equal(original.Y, point.Y);
    }

    [Fact]
    public void WKTFromShape_PointM_WithMetadata_RoundTrip()
    {
        var original = new PointM(15, 47, 350, 21.1);
        var wkt = original.WKTFromShape(WKTFormat.WKTWithMetadata);
        var restored = wkt.ShapeFromWKT();

        var point = Assert.IsType<PointM>(restored);
        Assert.Equal(original.X, point.X);
        Assert.Equal(original.Y, point.Y);
        Assert.Equal(original.Z, point.Z);
        Assert.Equal(21.1, Convert.ToDouble(point.M, _nhi));
    }

    #endregion

    #region MultiPoint

    [Fact]
    public void ShapeFromWKT_MultiPoint_ReturnsMultiPoint()
    {
        var shape = "MULTIPOINT((10 20),(30 40),(50 60))".ShapeFromWKT();

        var mp = Assert.IsType<MultiPoint>(shape);
        Assert.Equal(3, mp.PointCount);
        Assert.Equal(10, mp[0].X);
        Assert.Equal(20, mp[0].Y);
        Assert.Equal(30, mp[1].X);
        Assert.Equal(40, mp[1].Y);
        Assert.Equal(50, mp[2].X);
        Assert.Equal(60, mp[2].Y);
    }

    [Fact]
    public void ShapeFromWKT_MultiPoint_WithZAndM_ReturnsPointMValues()
    {
        var shape = "MULTIPOINT((15 47 z:350 m:21.1),(16 48 z:400 m:22.2))".ShapeFromWKT();

        var mp = Assert.IsType<MultiPoint>(shape);
        Assert.Equal(2, mp.PointCount);

        var p0 = Assert.IsType<PointM>(mp[0]);
        Assert.Equal(15, p0.X);
        Assert.Equal(47, p0.Y);
        Assert.Equal(350, p0.Z);
        Assert.Equal(21.1, Convert.ToDouble(p0.M, _nhi));

        var p1 = Assert.IsType<PointM>(mp[1]);
        Assert.Equal(16, p1.X);
        Assert.Equal(48, p1.Y);
        Assert.Equal(400, p1.Z);
        Assert.Equal(22.2, Convert.ToDouble(p1.M, _nhi));
    }

    [Fact]
    public void WKTFromShape_MultiPoint_RoundTrip()
    {
        var mp = new MultiPoint();
        mp.AddPoint(new Point(10, 20));
        mp.AddPoint(new Point(30, 40));

        var wkt = mp.WKTFromShape();
        var restored = wkt.ShapeFromWKT();

        var restoredMp = Assert.IsType<MultiPoint>(restored);
        Assert.Equal(2, restoredMp.PointCount);
        Assert.Equal(10, restoredMp[0].X);
        Assert.Equal(20, restoredMp[0].Y);
        Assert.Equal(30, restoredMp[1].X);
        Assert.Equal(40, restoredMp[1].Y);
    }

    #endregion

    #region Polyline / MultiLineString

    [Fact]
    public void ShapeFromWKT_LineString_ReturnsPolyline()
    {
        var shape = "LINESTRING(0 0,1 1,2 2)".ShapeFromWKT();

        var polyline = Assert.IsType<Polyline>(shape);
        Assert.Equal(1, polyline.PathCount);
        Assert.Equal(3, polyline[0].PointCount);
    }

    [Fact]
    public void ShapeFromWKT_MultiLineString_ReturnsPolylineWithMultiplePaths()
    {
        var shape = "MULTILINESTRING((0 0,1 1,2 2),(10 10,11 11,12 12))".ShapeFromWKT();

        var polyline = Assert.IsType<Polyline>(shape);
        Assert.Equal(2, polyline.PathCount);
        Assert.Equal(3, polyline[0].PointCount);
        Assert.Equal(3, polyline[1].PointCount);
    }

    [Fact]
    public void ShapeFromWKT_MultiLineString_WithZAndM_ParsesCorrectly()
    {
        var shape = "MULTILINESTRING((15 47 z:350 m:21.1,16 48 z:360 m:22.2))".ShapeFromWKT();

        var polyline = Assert.IsType<Polyline>(shape);
        Assert.Equal(1, polyline.PathCount);

        var path = polyline[0];
        Assert.Equal(2, path.PointCount);

        var p0 = Assert.IsType<PointM>(path[0]);
        Assert.Equal(15, p0.X);
        Assert.Equal(47, p0.Y);
        Assert.Equal(350, p0.Z);
        Assert.Equal(21.1, Convert.ToDouble(p0.M, _nhi));
    }

    [Fact]
    public void WKTFromShape_Polyline_RoundTrip()
    {
        var polyline = new Polyline();
        var path = new GeoPath();
        path.AddPoint(new Point(0, 0));
        path.AddPoint(new Point(1, 1));
        path.AddPoint(new Point(2, 2));
        polyline.AddPath(path);

        var wkt = polyline.WKTFromShape();
        var restored = wkt.ShapeFromWKT();

        var restoredPolyline = Assert.IsType<Polyline>(restored);
        Assert.Equal(1, restoredPolyline.PathCount);
        Assert.Equal(3, restoredPolyline[0].PointCount);
        Assert.Equal(0, restoredPolyline[0][0].X);
        Assert.Equal(0, restoredPolyline[0][0].Y);
        Assert.Equal(2, restoredPolyline[0][2].X);
        Assert.Equal(2, restoredPolyline[0][2].Y);
    }

    [Fact]
    public void WKTFromShape_MultiPolyline_RoundTrip()
    {
        var polyline = new Polyline();
        var path1 = new GeoPath();
        path1.AddPoint(new Point(0, 0));
        path1.AddPoint(new Point(1, 1));
        polyline.AddPath(path1);
        var path2 = new GeoPath();
        path2.AddPoint(new Point(10, 10));
        path2.AddPoint(new Point(11, 11));
        polyline.AddPath(path2);

        var wkt = polyline.WKTFromShape();
        var restored = wkt.ShapeFromWKT();

        var restoredPolyline = Assert.IsType<Polyline>(restored);
        Assert.Equal(2, restoredPolyline.PathCount);
    }

    #endregion

    #region Polygon / MultiPolygon

    [Fact]
    public void ShapeFromWKT_Polygon_ReturnsPolygon()
    {
        var shape = "POLYGON((0 0,1 0,1 1,0 1,0 0))".ShapeFromWKT();

        var polygon = Assert.IsType<Polygon>(shape);
        Assert.Equal(1, polygon.RingCount);
    }

    [Fact]
    public void ShapeFromWKT_MultiPolygon_ReturnsPolygonWithMultipleRings()
    {
        var shape = "MULTIPOLYGON(((0 0,1 0,1 1,0 1,0 0)),((10 10,11 10,11 11,10 11,10 10)))".ShapeFromWKT();

        var polygon = Assert.IsType<Polygon>(shape);
        Assert.Equal(2, polygon.RingCount);
    }

    [Fact]
    public void ShapeFromWKT_Polygon_WithZAndM_ParsesCorrectly()
    {
        var shape = "POLYGON((0 0 z:100 m:1,1 0 z:110 m:2,1 1 z:120 m:3,0 1 z:130 m:4,0 0 z:100 m:1))".ShapeFromWKT();

        var polygon = Assert.IsType<Polygon>(shape);
        Assert.Equal(1, polygon.RingCount);

        var ring = polygon[0];
        var p0 = Assert.IsType<PointM>(ring[0]);
        Assert.Equal(0, p0.X);
        Assert.Equal(0, p0.Y);
        Assert.Equal(100, p0.Z);
        Assert.Equal(1.0, Convert.ToDouble(p0.M, _nhi));
    }

    [Fact]
    public void WKTFromShape_Polygon_RoundTrip()
    {
        var polygon = new Polygon();
        var ring = new Ring();
        ring.AddPoint(new Point(0, 0));
        ring.AddPoint(new Point(1, 0));
        ring.AddPoint(new Point(1, 1));
        ring.AddPoint(new Point(0, 1));
        polygon.AddRing(ring);

        var wkt = polygon.WKTFromShape();
        var restored = wkt.ShapeFromWKT();

        var restoredPolygon = Assert.IsType<Polygon>(restored);
        Assert.Equal(1, restoredPolygon.RingCount);
    }

    [Fact]
    public void WKTFromShape_MultiPolygon_RoundTrip()
    {
        var polygon = new Polygon();
        var ring1 = new Ring();
        ring1.AddPoint(new Point(0, 0));
        ring1.AddPoint(new Point(1, 0));
        ring1.AddPoint(new Point(1, 1));
        ring1.AddPoint(new Point(0, 1));
        polygon.AddRing(ring1);
        var ring2 = new Ring();
        ring2.AddPoint(new Point(10, 10));
        ring2.AddPoint(new Point(11, 10));
        ring2.AddPoint(new Point(11, 11));
        ring2.AddPoint(new Point(10, 11));
        polygon.AddRing(ring2);

        var wkt = polygon.WKTFromShape();
        var restored = wkt.ShapeFromWKT();

        var restoredPolygon = Assert.IsType<Polygon>(restored);
        Assert.Equal(2, restoredPolygon.RingCount);
    }

    #endregion

    #region Envelope

    [Fact]
    public void WKTFromShape_Envelope_ProducesPolygonWKT()
    {
        var env = new Envelope(0, 0, 10, 10);
        var wkt = env.WKTFromShape();

        Assert.StartsWith("POLYGON((", wkt);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void ShapeFromWKT_InvalidWKT_ThrowsException()
        => Assert.Throws<Exception>(() => "POINT(1 2".ShapeFromWKT());
    

    [Fact]
    public void ShapeFromWKT_Point_SrsId_ParsesCorrectly()
    {
        var shape = "POINT(15 47 srs:4326)".ShapeFromWKT();

        var point = Assert.IsType<Point>(shape);
        Assert.Equal(15, point.X);
        Assert.Equal(47, point.Y);
        Assert.Equal(4326, point.SrsId);
    }

    [Fact]
    public void ShapeFromWKT_Point_WithZMAndM2_ReturnsPointM2()
    {
        var shape = "POINT(15 47 z:350 m:21.1 m2:5.5)".ShapeFromWKT();

        var point = Assert.IsType<PointM2>(shape);
        Assert.Equal(15, point.X);
        Assert.Equal(47, point.Y);
        Assert.Equal(350, point.Z);
        Assert.Equal(21.1, Convert.ToDouble(point.M, _nhi));
        Assert.Equal(5.5, Convert.ToDouble(point.M2, _nhi));
    }

    [Fact]
    public void ShapeFromWKT_IsCaseInsensitive()
    {
        var shape = "point(15 47)".ShapeFromWKT();
        Assert.IsType<Point>(shape);

        var shape2 = "multilinestring((0 0,1 1))".ShapeFromWKT();
        Assert.IsType<Polyline>(shape2);
    }

    #endregion
}
