using System.Globalization;
using System.Reflection;
using System.Xml;

using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebGIS.Tools.Editing.Environment.Services;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;

using static E.Standard.WebGIS.Tools.Editing.Environment.EditEnvironment;

namespace E.Standard.WebGIS.Tools.Tests.Editing.Environment.Services;

public class EditAutoValueServiceTests
{
    private const string EditNamespace = "http://www.e-steiermark.com/webgis/edit";
    private const string WebGisNamespace = "http://www.e-steiermark.com/webgis";

    [Fact]
    public async Task NullEditTheme_DoesNotSetValue()
    {
        var service = new EditAutoValueService(
            CreateEditEnvironment(),
            EditFeatureCommand.Insert,
            "TARGET",
            editTheme: null!,
            "guid",
            feature: null!,
            custom1: null!,
            custom2: null!);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(String.Empty, result.value);
        Assert.False(result.setIt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("mask-insert-default::value")]
    public async Task UnsupportedInput_DoesNotSetValue(string? autoValue)
    {
        var service = CreateService(autoValue);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(String.Empty, result.value);
        Assert.False(result.setIt);
    }

    [Fact]
    public async Task Expression_ReplacesFeatureAndGeometryParameters()
    {
        var feature = new Feature(
        [
            new WebMapping.Core.Attribute("NAME", "Main")
        ])
        {
            Shape = new Polyline(
            [
                new Point(0, 0),
                new Point(3, 4)
            ])
        };
        var service = CreateService(
            "=Road [NAME], length [:shape_len_int]",
            feature: feature);

        var result = await service.GetAutoValueAsync();

        Assert.Equal("Road Main, length 5", result.value);
        Assert.True(result.setIt);
    }

    [Fact]
    public async Task Scale_ReturnsRoundedCurrentMapScale()
    {
        var editEnvironment = CreateEditEnvironment();
        editEnvironment.CurrentMapScale = 1234.6;
        var service = CreateService("scale", editEnvironment: editEnvironment);

        var result = await service.GetAutoValueAsync();

        Assert.Equal("1235", result.value);
        Assert.True(result.setIt);
    }

    [Theory]
    [InlineData((int)EditFeatureCommand.Insert, "insert")]
    [InlineData((int)EditFeatureCommand.Update, "update")]
    [InlineData((int)EditFeatureCommand.Delete, "delete")]
    [InlineData((int)EditFeatureCommand.MassAttribution, "mass_attribution")]
    [InlineData((int)EditFeatureCommand.Transfer, "transfer")]
    public async Task EditOperation_ReturnsStableOperationName(
        int commandValue,
        string expectedValue)
    {
        var service = CreateService(
            "edit_operation",
            (EditFeatureCommand)commandValue);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
    }

    [Theory]
    [InlineData("map_srefid", "31256")]
    [InlineData("edit_service_id", "service-1")]
    [InlineData("edit_layer_id", "layer-2")]
    [InlineData("edit_theme_id", "theme-3")]
    public async Task EditContext_ReturnsEnvironmentValue(
        string autoValue,
        string expectedValue)
    {
        var editEnvironment = CreateEditEnvironment(
            editThemeDefinition: new EditThemeDefinition
            {
                ServiceId = "service-1",
                LayerId = "layer-2",
                EditThemeId = "theme-3"
            });
        editEnvironment.CurrentMapSrsId = 31256;
        var service = CreateService(
            autoValue,
            editEnvironment: editEnvironment);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
    }

    [Theory]
    [InlineData("edit_service_id")]
    [InlineData("edit_layer_id")]
    [InlineData("edit_theme_id")]
    public async Task EditContext_WithoutThemeDefinition_ReturnsEmptyValue(
        string autoValue)
    {
        var service = CreateService(autoValue);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(String.Empty, result.value);
        Assert.True(result.setIt);
    }

    [Fact]
    public async Task Guid_OnInsert_ReturnsGuidWithoutSeparators()
    {
        var service = CreateService("guid", EditFeatureCommand.Insert);

        var result = await service.GetAutoValueAsync();

        Assert.True(result.setIt);
        Assert.Equal(32, result.value.Length);
        Assert.True(Guid.TryParseExact(result.value, "N", out _));
    }

    [Fact]
    public async Task Guid_OnUpdate_DoesNotSetValue()
    {
        var service = CreateService("guid", EditFeatureCommand.Update);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(String.Empty, result.value);
        Assert.False(result.setIt);
    }

    [Theory]
    [InlineData("shape_minx", "100")]
    [InlineData("shape_miny", "200")]
    [InlineData("shape_maxx", "300")]
    [InlineData("shape_maxy", "400")]
    public async Task ShapeCoordinate_WithoutTargetSRef_ReturnsOriginalCoordinate(
        string autoValue,
        string expectedValue)
    {
        var feature = CreateFeatureWithEnvelope();
        var service = CreateService(autoValue, feature: feature);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
    }

    [Theory]
    [InlineData("shape_minx:4326", "110")]
    [InlineData("shape_miny:4326", "220")]
    [InlineData("shape_maxx:4326", "310")]
    [InlineData("shape_maxy:4326", "420")]
    public async Task ShapeCoordinate_WithTargetSRef_ReturnsTransformedCoordinate(
        string autoValue,
        string expectedValue)
    {
        var bridgeProxy = new BridgeProxy
        {
            GeometryTransformerFactory = (fromSRefId, toSRefId) =>
            {
                Assert.Equal(3857, fromSRefId);
                Assert.Equal(4326, toSRefId);
                return new OffsetGeometryTransformer(10, 20);
            }
        };
        var editEnvironment = CreateEditEnvironment(bridgeProxy: bridgeProxy);
        var feature = CreateFeatureWithEnvelope();
        var service = CreateService(
            autoValue,
            feature: feature,
            editEnvironment: editEnvironment);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
        Assert.Equal(100, feature.Shape.ShapeEnvelope.MinX);
        Assert.Equal(200, feature.Shape.ShapeEnvelope.MinY);
    }

    [Fact]
    public async Task ShapeCoordinate_WithInvalidTargetSRef_ThrowsConfigurationError()
    {
        var service = CreateService(
            "shape_minx:not-an-epsg",
            feature: CreateFeatureWithEnvelope());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            service.GetAutoValueAsync);

        Assert.Equal(
            "Autovalue shape_minx:not-an-epsg: Invalid target SRefId",
            exception.Message);
    }

    [Fact]
    public async Task ShapeCoordinate_WithTargetSRefButNoSourceSRef_ThrowsConfigurationError()
    {
        var feature = CreateFeatureWithEnvelope();
        feature.Shape.SrsId = 0;
        var service = CreateService("shape_minx:4326", feature: feature);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            service.GetAutoValueAsync);

        Assert.Equal(
            "Autovalue shape_minx:4326: Source SRefId is not set",
            exception.Message);
    }

    [Theory]
    [InlineData("shape_len:4326", "13.42")]
    [InlineData("shape_len_int:4326", "13")]
    public async Task ShapeLength_WithTargetSRef_IsCalculatedFromTransformedGeometry(
        string autoValue,
        string expectedValue)
    {
        var editEnvironment = CreateEditEnvironment(
            bridgeProxy: CreateScalingBridgeProxy());
        var feature = new Feature
        {
            Shape = new Polyline(
            [
                new Point(0, 0),
                new Point(3, 4)
            ])
            {
                SrsId = 3857
            }
        };
        var service = CreateService(
            autoValue,
            feature: feature,
            editEnvironment: editEnvironment);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
        Assert.Equal(5, ((Polyline)feature.Shape).Length);
        Assert.Equal(3857, feature.Shape.SrsId);
    }

    [Theory]
    [InlineData("shape_area:4326", "600")]
    [InlineData("shape_area_int:4326", "600")]
    public async Task ShapeArea_WithTargetSRef_IsCalculatedFromTransformedGeometry(
        string autoValue,
        string expectedValue)
    {
        var editEnvironment = CreateEditEnvironment(
            bridgeProxy: CreateScalingBridgeProxy());
        var feature = new Feature
        {
            Shape = new Polygon(
                new Ring(
                [
                    new Point(0, 0),
                    new Point(10, 0),
                    new Point(10, 10),
                    new Point(0, 10),
                    new Point(0, 0)
                ]))
            {
                SrsId = 3857
            }
        };
        var service = CreateService(
            autoValue,
            feature: feature,
            editEnvironment: editEnvironment);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
        Assert.Equal(100, ((Polygon)feature.Shape).Area);
        Assert.Equal(3857, feature.Shape.SrsId);
    }

    [Theory]
    [InlineData("shape_perimeter:4326", "100")]
    [InlineData("shape_centroid_x:4326", "10")]
    [InlineData("shape_centroid_y:4326", "15")]
    public async Task PolygonMetadata_WithTargetSRef_UsesTransformedGeometry(
        string autoValue,
        string expectedValue)
    {
        var editEnvironment = CreateEditEnvironment(
            bridgeProxy: CreateScalingBridgeProxy());
        var feature = CreateSquarePolygonFeature();
        var service = CreateService(
            autoValue,
            feature: feature,
            editEnvironment: editEnvironment);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
        Assert.Equal(100, ((Polygon)feature.Shape).Area);
        Assert.Equal(3857, feature.Shape.SrsId);
    }

    [Theory]
    [InlineData("shape_vertex_count", "5")]
    [InlineData("shape_part_count", "1")]
    [InlineData("shape_type", "polygon")]
    [InlineData("shape_srefid", "3857")]
    public async Task ShapeMetadata_ReturnsExpectedValue(
        string autoValue,
        string expectedValue)
    {
        var service = CreateService(
            autoValue,
            feature: CreateSquarePolygonFeature());

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
    }

    [Theory]
    [InlineData("shape_centroid_x", "10")]
    [InlineData("shape_centroid_y", "0")]
    [InlineData("shape_vertex_count", "3")]
    [InlineData("shape_part_count", "1")]
    [InlineData("shape_type", "polyline")]
    public async Task PolylineMetadata_ReturnsExpectedValue(
        string autoValue,
        string expectedValue)
    {
        var feature = new Feature
        {
            Shape = new Polyline(
            [
                new Point(0, 0),
                new Point(10, 0),
                new Point(10, 10)
            ])
        };
        var service = CreateService(autoValue, feature: feature);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
    }

    [Theory]
    [InlineData("shape_centroid_x", "5")]
    [InlineData("shape_centroid_y", "10")]
    [InlineData("shape_vertex_count", "2")]
    [InlineData("shape_part_count", "2")]
    [InlineData("shape_type", "multipoint")]
    public async Task MultiPointMetadata_ReturnsExpectedValue(
        string autoValue,
        string expectedValue)
    {
        var feature = new Feature
        {
            Shape = new MultiPoint(
            [
                new Point(0, 0),
                new Point(10, 20)
            ])
        };
        var service = CreateService(autoValue, feature: feature);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
    }

    [Theory]
    [InlineData("shape_centroid_x", "7")]
    [InlineData("shape_centroid_y", "9")]
    [InlineData("shape_vertex_count", "1")]
    [InlineData("shape_part_count", "1")]
    [InlineData("shape_type", "point")]
    public async Task PointMetadata_ReturnsExpectedValue(
        string autoValue,
        string expectedValue)
    {
        var feature = new Feature
        {
            Shape = new Point(7, 9)
        };
        var service = CreateService(autoValue, feature: feature);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedValue, result.value);
        Assert.True(result.setIt);
    }

    [Fact]
    public async Task ShapeMetadata_WithUnsupportedTargetSRef_ThrowsConfigurationError()
    {
        var service = CreateService(
            "shape_vertex_count:4326",
            feature: CreateSquarePolygonFeature());

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            service.GetAutoValueAsync);

        Assert.Equal(
            "Autovalue shape_vertex_count:4326: Target SRefId is not supported",
            exception.Message);
    }

    [Fact]
    public async Task PolygonWithHole_PartCountExcludesHoleAndCentroidSubtractsItsArea()
    {
        var feature = new Feature
        {
            Shape = new Polygon(
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
            ])
        };

        var partCount = await CreateService(
            "shape_part_count",
            feature: feature).GetAutoValueAsync();
        var centroidX = await CreateService(
            "shape_centroid_x",
            feature: feature).GetAutoValueAsync();

        Assert.Equal("1", partCount.value);
        Assert.Equal(
            4.916666666666667,
            Double.Parse(centroidX.value, CultureInfo.InvariantCulture),
            precision: 12);
    }

    [Theory]
    [InlineData("create_datetime_utc", (int)EditFeatureCommand.Insert, true)]
    [InlineData("create_datetime_utc", (int)EditFeatureCommand.Update, false)]
    [InlineData("change_datetime_utc", (int)EditFeatureCommand.Insert, true)]
    [InlineData("change_datetime_utc", (int)EditFeatureCommand.Update, true)]
    public async Task UtcAuditValue_RespectsCommandAndReturnsIsoTimestamp(
        string autoValue,
        int commandValue,
        bool expectedSetIt)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");

        try
        {
            var before = DateTime.UtcNow;
            var service = CreateService(
                autoValue,
                (EditFeatureCommand)commandValue);

            var result = await service.GetAutoValueAsync();

            Assert.Equal(expectedSetIt, result.setIt);
            if (expectedSetIt)
            {
                var timestamp = DateTime.ParseExact(
                    result.value,
                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                Assert.InRange(timestamp, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
            }
            else
            {
                Assert.Equal(String.Empty, result.value);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task UrlParameter_ReturnsOriginalUrlParameter()
    {
        var editEnvironment = CreateEditEnvironment(
            new Dictionary<string, string>
            {
                ["source"] = "from-url"
            });
        var service = CreateService(
            "url-parameter:source",
            EditFeatureCommand.Update,
            editEnvironment: editEnvironment);

        var result = await service.GetAutoValueAsync();

        Assert.Equal("from-url", result.value);
        Assert.True(result.setIt);
    }

    [Theory]
    [InlineData((int)EditFeatureCommand.Insert, true)]
    [InlineData((int)EditFeatureCommand.Update, false)]
    public async Task OnInsertUrlParameter_IsOnlyAppliedForInsert(
        int commandValue,
        bool expectedSetIt)
    {
        var command = (EditFeatureCommand)commandValue;
        var editEnvironment = CreateEditEnvironment(
            new Dictionary<string, string>
            {
                ["source"] = "from-url"
            });
        var service = CreateService(
            "oninsert:url-parameter:source",
            command,
            editEnvironment: editEnvironment);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(expectedSetIt ? "from-url" : String.Empty, result.value);
        Assert.Equal(expectedSetIt, result.setIt);
    }

    [Fact]
    public async Task DbSelect_WithoutConnectionString_ThrowsConfigurationError()
    {
        var service = CreateService(
            "db_select",
            custom1: null,
            custom2: "select 1");

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            service.GetAutoValueAsync);

        Assert.Equal(
            "Autovalue db_select: ConnectionString not set! Set ConnectionString in autovalue_custom1!",
            exception.Message);
    }

    [Fact]
    public async Task DbSelectOnInsert_OnUpdate_DoesNotValidateOrSetValue()
    {
        var service = CreateService(
            "db_select_on_insert",
            EditFeatureCommand.Update);

        var result = await service.GetAutoValueAsync();

        Assert.Equal(String.Empty, result.value);
        Assert.False(result.setIt);
    }

    private static EditAutoValueService CreateService(
        string? autoValue,
        EditFeatureCommand command = EditFeatureCommand.Insert,
        Feature? feature = null,
        EditEnvironment.EditTheme? editTheme = default,
        EditEnvironment? editEnvironment = null,
        string? custom1 = null,
        string? custom2 = null)
    {
        editEnvironment ??= CreateEditEnvironment();
        editTheme ??= CreateEditTheme(editEnvironment);

        return new EditAutoValueService(
            editEnvironment,
            command,
            "TARGET",
            editTheme,
            autoValue!,
            feature!,
            custom1!,
            custom2!);
    }

    private static EditEnvironment CreateEditEnvironment(
        IReadOnlyDictionary<string, string>? originalUrlParameters = null,
        BridgeProxy? bridgeProxy = null,
        EditThemeDefinition? editThemeDefinition = null)
    {
        var bridge = DispatchProxy.Create<IBridge, BridgeProxy>();
        var proxy = (BridgeProxy)(object)bridge;
        proxy.OriginalUrlParameters =
            originalUrlParameters
            ?? bridgeProxy?.OriginalUrlParameters
            ?? new Dictionary<string, string>();
        proxy.GeometryTransformerFactory = bridgeProxy?.GeometryTransformerFactory;

        return new EditEnvironment(bridge, editThemeDefinition!);
    }

    private static Feature CreateFeatureWithEnvelope()
        => new()
        {
            Shape = new Polyline(
            [
                new Point(100, 200),
                new Point(300, 400)
            ])
            {
                SrsId = 3857
            }
        };

    private static Feature CreateSquarePolygonFeature()
        => new()
        {
            Shape = new Polygon(
                new Ring(
                [
                    new Point(0, 0),
                    new Point(10, 0),
                    new Point(10, 10),
                    new Point(0, 10),
                    new Point(0, 0)
                ]))
            {
                SrsId = 3857
            }
        };

    private static BridgeProxy CreateScalingBridgeProxy()
        => new()
        {
            GeometryTransformerFactory = (fromSRefId, toSRefId) =>
            {
                Assert.Equal(3857, fromSRefId);
                Assert.Equal(4326, toSRefId);
                return new OffsetGeometryTransformer(
                    xOffset: 0,
                    yOffset: 0,
                    xScale: 2,
                    yScale: 3);
            }
        };

    private static EditTheme CreateEditTheme(EditEnvironment editEnvironment)
    {
        string xml = $"""
            <editthemes xmlns:edit="{EditNamespace}" xmlns:webgis="{WebGisNamespace}">
              <edit:edittheme id="theme1">
                <edit:mask />
              </edit:edittheme>
            </editthemes>
            """;

        var document = new XmlDocument();
        document.LoadXml(xml);

        var namespaces = new XmlNamespaceManager(document.NameTable);
        namespaces.AddNamespace("webgis", WebGisNamespace);
        namespaces.AddNamespace("edit", EditNamespace);

        var themeNode = document.SelectSingleNode(
            "editthemes/edit:edittheme",
            namespaces);

        return new EditTheme(editEnvironment, themeNode!, namespaces);
    }

    public class BridgeProxy : DispatchProxy
    {
        public BridgeProxy()
        {
        }

        public IReadOnlyDictionary<string, string> OriginalUrlParameters { get; set; } =
            new Dictionary<string, string>();

        public Func<int, int, IGeometricTransformer>? GeometryTransformerFactory { get; set; }

        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? args)
        {
            if (targetMethod?.Name == "get_AppAssemblyPath"
                || targetMethod?.Name == "get_AppEtcPath")
            {
                return String.Empty;
            }

            if (targetMethod?.Name == nameof(IBridge.GetOriginalUrlParameterValue))
            {
                var parameterName = (string)args![0]!;
                return OriginalUrlParameters.TryGetValue(parameterName, out var value)
                    ? value
                    : null;
            }

            if (targetMethod?.Name == nameof(IBridge.GeometryTransformer))
            {
                return GeometryTransformerFactory!(
                    (int)args![0]!,
                    (int)args[1]!);
            }

            return targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }

    private sealed class OffsetGeometryTransformer(
        double xOffset,
        double yOffset,
        double xScale = 1,
        double yScale = 1) : IGeometricTransformer
    {
        public void Transform(double[] x, double[] y)
        {
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = x[i] * xScale + xOffset;
                y[i] = y[i] * yScale + yOffset;
            }
        }

        public void Transform(Shape shape)
        {
            switch (shape)
            {
                case Envelope envelope:
                    envelope.Set(
                        envelope.MinX * xScale + xOffset,
                        envelope.MinY * yScale + yOffset,
                        envelope.MaxX * xScale + xOffset,
                        envelope.MaxY * yScale + yOffset);
                    return;
                case Polyline polyline:
                    for (int pathIndex = 0; pathIndex < polyline.PathCount; pathIndex++)
                    {
                        Transform(polyline[pathIndex]);
                    }
                    return;
                case Polygon polygon:
                    for (int ringIndex = 0; ringIndex < polygon.RingCount; ringIndex++)
                    {
                        Transform(polygon[ringIndex]);
                    }
                    return;
                default:
                    throw new NotSupportedException();
            }
        }

        public void InvTransform(Shape shape) => throw new NotSupportedException();

        public void Dispose()
        {
        }

        private void Transform(PointCollection points)
        {
            for (int pointIndex = 0; pointIndex < points.PointCount; pointIndex++)
            {
                points[pointIndex].X =
                    points[pointIndex].X * xScale + xOffset;
                points[pointIndex].Y =
                    points[pointIndex].Y * yScale + yOffset;
            }
        }
    }
}
