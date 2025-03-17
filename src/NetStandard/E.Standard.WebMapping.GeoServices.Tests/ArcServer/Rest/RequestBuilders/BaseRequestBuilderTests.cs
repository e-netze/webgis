using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.DynamicLayers;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.RequestBuilders;

namespace E.Standard.WebMapping.GeoServices.Tests.ArcServer.Rest.RequestBuilders;

public class BaseRequestBuilderTests
{
    [Fact]
    public void WithFormat_ShouldAppendFormatParameter()
    {
        // Arrange
        var builder = new ExportRequestBuilder();

        // Act
        builder.WithFormat("json");
        var result = builder.Build();

        // Assert
        Assert.Equal("f=json", result);
    }

    [Fact]
    public void WithBBox_ShouldAppendBBoxParameter()
    {
        // Arrange
        var builder = new ExportRequestBuilder();
        var mockEnvelope = new Envelope(10, 20, 30, 40);

        // Act
        builder.WithBBox(mockEnvelope);
        var result = builder.Build();

        // Assert
        Assert.Equal("bbox=10,20,30,40", result);
    }

    [Fact]
    public void WithBBox_ShouldAppendBBoxParameterWithDoubles()
    {
        // Arrange
        var builder = new ExportRequestBuilder();
        var mockEnvelope = new Envelope(10.1D, 20.2D, 30.3D, 40.4D);

        // Act
        builder.WithBBox(mockEnvelope);
        var result = builder.Build();

        // Assert
        Assert.Equal("bbox=10.1,20.2,30.3,40.4", result);
    }

    [Fact]
    public void WithLayers_ShouldHandleNullLayerIds()
    {
        // Arrange
        var builder = new ExportRequestBuilder();

        // Act
        builder.WithLayers((IEnumerable<int>?)null);
        var result = builder.Build();

        // Assert
        Assert.Equal("layers=", result);
    }

    [Fact]
    public void WithLayers_ShouldAppendIntegerLayerIds()
    {
        // Arrange
        var builder = new ExportRequestBuilder();
        var layerIds = new List<int> { 1, 2, 3 };

        // Act
        builder.WithLayers(layerIds);
        var result = builder.Build();

        // Assert
        Assert.Equal("layers=show:1,2,3", result);
    }

    [Fact]
    public void WithImageSizeAndDpi_ShouldAppendSizeAndDpiParameters()
    {
        // Arrange
        var builder = new ExportRequestBuilder();

        // Act
        builder.WithImageSizeAndDpi(1920, 1080, 96);
        var result = builder.Build();

        // Assert
        Assert.Equal("size=1920,1080&dpi=96", result);
    }

    [Fact]
    public void WithGeometry_ShouldAppendGeometryAndType()
    {
        // Arrange
        var builder = new GetFeaturesRequestBuilder();
        var mockPoint = new Point(10, 20) { SrsId = 4326 };

        // Act
        builder.WithGeometry(mockPoint);
        var result = builder.Build();

        // Assert
        Assert.Contains("geometry=", result);
        Assert.Contains("geometryType=esriGeometryPoint", result);
    }

    [Fact]
    public void WithDatumTransformations_ShouldAppendTransformations()
    {
        // Arrange
        var builder = new ExportRequestBuilder();
        var transformations = new int[] { 1001, 1002 };

        // Act
        builder.WithDatumTransformations(transformations);
        var result = builder.Build();

        // Assert
        Assert.Equal("datumTransformations=[1001,1002]", result);
    }

    [Fact]
    public void WithDatumTransformations_ShouldAppendTransformation()
    {
        // Arrange
        var builder = new GetFeaturesRequestBuilder();

        // Act
        builder.WithDatumTransformation(1001);
        var result = builder.Build();

        // Assert
        Assert.Equal("datumTransformation=1001", result);
    }

    [Fact]
    public void WithReturnGeometry_ShouldAppendCorrectParameter()
    {
        // Arrange
        var builder = new GetFeaturesRequestBuilder();

        // Act
        builder.WithReturnGeometry(true);
        var result1 = builder.Build();

        builder = new GetFeaturesRequestBuilder();
        builder.WithReturnGeometry(false);
        var result2 = builder.Build();

        // Assert
        Assert.Equal("returnGeometry=true", result1);
        Assert.Equal("returnGeometry=false", result2);
    }

    [Fact]
    public void WithDynamicLayers_ShouldAppendDynamicLayersParameter()
    {
        // Arrange
        var builder = new ExportRequestBuilder();
        var layers = new List<DynamicLayer>
        {
            new DynamicLayer
            {
                id = 1,
                source = new DynamicLayerSouce { mapLayerId = 10 },
                definitionExpression = "OBJECTID > 1000",
                drawingInfo = new DynamicLayerDrawingInfo
                {
                    transparency = 50,
                    scaleSymbols = true,
                    showLabels = false,
                    labelingInfo = new[]
                    {
                        new LabelingInfo
                        {
                            LabelPlacement = "esriServerPointLabelPlacementAboveRight",
                            LabelExpression = "[NAME]",
                            UseCodedValues = true
                        }
                    }
                }
            },
            new DynamicLayer
            {
                id = 2,
                source = new DynamicLayerSouce { mapLayerId = 20 },
                drawingInfo = new DynamicLayerDrawingInfo
                {
                    transparency = 75
                }
            }
        };

        // Act
        builder.WithDynamicLayers(layers);
        var result = builder.Build();

        // Assert
        Assert.Contains("dynamicLayers", result);
        Assert.Contains("OBJECTID > 1000", result);
        Assert.Contains(@"""transparency"":50", result);
        Assert.Contains(@"""mapLayerId"":10", result);
        Assert.Contains(@"""mapLayerId"":20", result);
    }
}