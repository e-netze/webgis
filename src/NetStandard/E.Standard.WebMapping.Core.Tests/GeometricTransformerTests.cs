using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.Core.Tests;

public class GeometricTransformerTests
{
    private static readonly SpatialReference Wgs84 = new(
        4326,
        "WGS 84",
        "+proj=longlat +datum=WGS84 +no_defs");

    private static readonly SpatialReference WebMercator = new(
        3857,
        "Web Mercator",
        "+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 "
        + "+x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext +no_defs");

    [Fact]
    public void Transform2D_WithSpatialReferences_SetsTargetSrsId()
    {
        var point = new Point(13, 48) { SrsId = Wgs84.Id };
        using var transformer = new GeometricTransformer();
        transformer.FromSpatialReference(Wgs84);
        transformer.ToSpatialReference(WebMercator);

        transformer.Transform2D(point);

        Assert.Equal(WebMercator.Id, point.SrsId);
    }

    [Fact]
    public void InvTransform2D_WithSpatialReferences_SetsSourceSrsId()
    {
        var point = new Point(1447153.38, 6106854.83) { SrsId = WebMercator.Id };
        using var transformer = new GeometricTransformer();
        transformer.FromSpatialReference(Wgs84);
        transformer.ToSpatialReference(WebMercator);

        transformer.InvTransform2D(point);

        Assert.Equal(Wgs84.Id, point.SrsId);
    }

    [Fact]
    public void Transform2D_WithStringConfiguration_DoesNotInventSrsId()
    {
        Shape point = new Point(13, 48) { SrsId = 1234 };

        GeometricTransformer.Transform2D(
            point,
            Wgs84.Proj4,
            !Wgs84.IsProjective,
            WebMercator.Proj4,
            !WebMercator.IsProjective);

        Assert.Equal(1234, point.SrsId);
    }

    [Fact]
    public void ProTransformer_WithSpatialReferences_UsesTargetSrsId()
    {
        var point = new Point(13, 48) { SrsId = Wgs84.Id };
        using var transformer = new GeometricTransformerPro(Wgs84, WebMercator);

        transformer.Transform(point);

        Assert.Equal(Wgs84.Id, transformer.FromSrsId);
        Assert.Equal(WebMercator.Id, transformer.ToSrsId);
        Assert.Equal(WebMercator.Id, point.SrsId);
    }

    [Fact]
    public void ProTransformer_InvTransform_UsesSourceSrsId()
    {
        var point = new Point(1447153.38, 6106854.83) { SrsId = WebMercator.Id };
        using var transformer = new GeometricTransformerPro(Wgs84, WebMercator);

        transformer.InvTransform(point);

        Assert.Equal(Wgs84.Id, point.SrsId);
    }

    [Fact]
    public void StaticTransform2D_WithSpatialReferences_SetsTargetSrsId()
    {
        Shape point = new Point(13, 48) { SrsId = Wgs84.Id };

        var transformed = GeometricTransformer.Transform2D(point, Wgs84, WebMercator);

        Assert.Same(point, transformed);
        Assert.Equal(WebMercator.Id, transformed.SrsId);
    }

    [Fact]
    public void ProTransformer_IdentityTransformation_SetsRequestedProperties()
    {
        var point = new Point(13, 48)
        {
            SrsId = 0,
            SrsP4Parameters = null
        };
        using var transformer = new GeometricTransformerPro(Wgs84, Wgs84);

        transformer.Transform(
            point,
            ShapeSrsProperties.SrsId | ShapeSrsProperties.SrsProj4Parameters);

        Assert.Equal(Wgs84.Id, point.SrsId);
        Assert.Equal(Wgs84.Proj4, point.SrsP4Parameters);
        Assert.Equal(13, point.X);
        Assert.Equal(48, point.Y);
    }

    [Fact]
    public void ProTransformer_None_DoesNotChangeShapeProperties()
    {
        var point = new Point(13, 48)
        {
            SrsId = 1234,
            SrsP4Parameters = "original"
        };
        using var transformer = new GeometricTransformerPro(Wgs84, WebMercator);

        transformer.Transform(point, ShapeSrsProperties.None);

        Assert.Equal(1234, point.SrsId);
        Assert.Equal("original", point.SrsP4Parameters);
    }

    [Fact]
    public void ProTransformer_Proj4Only_DoesNotChangeSrsId()
    {
        var point = new Point(13, 48)
        {
            SrsId = 1234,
            SrsP4Parameters = "original"
        };
        using var transformer = new GeometricTransformerPro(Wgs84, WebMercator);

        transformer.Transform(point, ShapeSrsProperties.SrsProj4Parameters);

        Assert.Equal(1234, point.SrsId);
        Assert.Equal(WebMercator.Proj4, point.SrsP4Parameters);
    }
}
