using System.Globalization;

using E.Standard.WebGIS.CMS.Expressions;

namespace E.Standard.WebGIS.CMS.Tests;

public class ExpressionPlaygroundServiceTests
{
    [Fact]
    public void Evaluate_AppliesStructuredTableColumnExpressionToEachFeature()
    {
        var results = ExpressionPlaygroundService.Evaluate(
            FeatureCollection,
            "concat([FIRSTNAME], \" \", [LASTNAME])",
            CmsExpressionType.TableColumn);

        Assert.Collection(
            results,
            first =>
            {
                Assert.Equal(1, first.Index);
                Assert.Equal("Ada Lovelace", first.Result);
                Assert.Null(first.Error);
            },
            second =>
            {
                Assert.Equal(2, second.Index);
                Assert.Equal("Grace Hopper", second.Result);
                Assert.Null(second.Error);
            });
    }

    [Fact]
    public void Evaluate_AppliesAutoValuePrefixAndLegacyTemplate()
    {
        var results = ExpressionPlaygroundService.Evaluate(
            FeatureCollection,
            " =Road [FIRSTNAME] ",
            CmsExpressionType.AutoValue);

        Assert.Equal("Road Ada", results[0].Result);
        Assert.Null(results[0].Error);
    }

    [Fact]
    public void Evaluate_AppliesStructuredAutoValueExpression()
    {
        var results = ExpressionPlaygroundService.Evaluate(
            FeatureCollection,
            "=concat([FIRSTNAME], \" \", [LASTNAME])",
            CmsExpressionType.AutoValue);

        Assert.Equal("Ada Lovelace", results[0].Result);
        Assert.Null(results[0].Error);
    }

    [Theory]
    [InlineData("=concat([FIRSTNAME], \" \", [LASTNAME])", CmsExpressionType.AutoValue, "StructuredExpression")]
    [InlineData("=Road [FIRSTNAME]", CmsExpressionType.AutoValue, "LegacyTemplate")]
    [InlineData("concat([FIRSTNAME], \" \", [LASTNAME])", CmsExpressionType.TableColumn, "StructuredExpression")]
    [InlineData("$round2([AREA])", CmsExpressionType.TableColumn, "LegacyTemplate")]
    public void ClassifySyntax_ReportsTheSelectedEvaluationPath(
        string expression,
        CmsExpressionType expressionType,
        string expectedSyntax)
    {
        Assert.Equal(
            expectedSyntax,
            ExpressionPlaygroundService.ClassifySyntax(expression, expressionType).ToString());
    }

    [Fact]
    public void Evaluate_DoesNotRemoveEqualsForTableColumnExpressions()
    {
        var results = ExpressionPlaygroundService.Evaluate(
            FeatureCollection,
            "=concat([FIRSTNAME], \" \", [LASTNAME])",
            CmsExpressionType.TableColumn);

        Assert.Equal("=concat(Ada, \" \", Lovelace)", results[0].Result);
    }

    [Fact]
    public void Evaluate_UsesLegacyTableColumnEvaluation()
    {
        var results = ExpressionPlaygroundService.Evaluate(
            FeatureCollection,
            "$round2([AREA])",
            CmsExpressionType.TableColumn);

        Assert.Equal(Math.Round(12345.678, 2).ToString("0.00"), results[0].Result);
    }

    [Fact]
    public void Evaluate_SupportsGeometryExpressions()
    {
        const string geoJson = """
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[0, 0], [10, 0], [10, 5], [0, 5], [0, 0]]]
              },
              "properties": {}
            }
            """;

        var results = ExpressionPlaygroundService.Evaluate(
            geoJson,
            "shape_area()",
            CmsExpressionType.TableColumn);

        Assert.Single(results);
        Assert.Equal("50", results[0].Result);
    }

    [Fact]
    public void Evaluate_ProjectsShapeForGeometryFunction()
    {
        const string geoJson = """
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[0, 0], [1, 0], [1, 1], [0, 1], [0, 0]]]
              },
              "properties": {}
            }
            """;

        var result = Assert.Single(ExpressionPlaygroundService.Evaluate(
            geoJson,
            "shape_area(3857)",
            CmsExpressionType.TableColumn));

        Assert.Null(result.Error);
        Assert.True(double.TryParse(
            result.Result,
            CultureInfo.InvariantCulture,
            out var area));
        Assert.InRange(area, 12_000_000_000, 13_000_000_000);
    }

    [Fact]
    public void Evaluate_ProjectsShapeToAustrianGrid()
    {
        const string geoJson = """
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[13, 47], [14, 47], [14, 48], [13, 48], [13, 47]]]
              },
              "properties": {}
            }
            """;

        var result = Assert.Single(ExpressionPlaygroundService.Evaluate(
            geoJson,
            "shape_area(31256)",
            CmsExpressionType.TableColumn));

        Assert.Null(result.Error);
        Assert.True(double.TryParse(
            result.Result,
            CultureInfo.InvariantCulture,
            out var area));
        Assert.True(area > 0);
    }

    [Fact]
    public void Evaluate_ReturnsAnErrorForEachFeatureWhenTheExpressionIsInvalid()
    {
        var results = ExpressionPlaygroundService.Evaluate(
            FeatureCollection,
            "unknown_function()",
            CmsExpressionType.TableColumn);

        Assert.Equal(2, results.Count);
        Assert.All(results, result =>
        {
            Assert.Null(result.Result);
            Assert.Contains("Unknown function", result.Error);
        });
    }

    [Fact]
    public void Evaluate_RejectsOtherGeoJsonRootTypes()
    {
        var exception = Assert.Throws<FormatException>(() =>
            ExpressionPlaygroundService.Evaluate(
                """{"type":"Point","coordinates":[0,0]}""",
                "1",
                CmsExpressionType.TableColumn));

        Assert.Contains("Feature or FeatureCollection", exception.Message);
    }

    [Fact]
    public void Evaluate_RejectsGeoJsonAboveTheConfiguredSizeLimit()
    {
        var geoJson = new string('x', ExpressionPlaygroundService.MaxGeoJsonSizeBytes + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            ExpressionPlaygroundService.Evaluate(
                geoJson,
                "1",
                CmsExpressionType.TableColumn));

        Assert.Contains("maximum size", exception.Message);
    }

    [Fact]
    public void Evaluate_RejectsFeatureCollectionsAboveTheConfiguredFeatureLimit()
    {
        var features = String.Join(
            ",",
            Enumerable.Repeat(
                """{"type":"Feature","geometry":null,"properties":{}}""",
                ExpressionPlaygroundService.MaxFeatureCount + 1));
        var geoJson = $$"""{"type":"FeatureCollection","features":[{{features}}]}""";

        var exception = Assert.Throws<ArgumentException>(() =>
            ExpressionPlaygroundService.Evaluate(
                geoJson,
                "1",
                CmsExpressionType.TableColumn));

        Assert.Contains("maximum of 1000 features", exception.Message);
    }

    [Fact]
    public void Evaluate_ReturnsAnErrorForMalformedGeometryCoordinates()
    {
        const string geoJson = """
            {
              "type": "Feature",
              "geometry": { "type": "Point", "coordinates": [0] },
              "properties": {}
            }
            """;

        var result = Assert.Single(ExpressionPlaygroundService.Evaluate(
            geoJson,
            "shape_centroid_x()",
            CmsExpressionType.TableColumn));

        Assert.Null(result.Result);
        Assert.Contains("at least two numbers", result.Error);
    }

    private const string FeatureCollection = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [[[0, 0], [10, 0], [10, 5], [0, 5], [0, 0]]]
              },
              "properties": {
                "FIRSTNAME": "Ada",
                "LASTNAME": "Lovelace",
                "AREA": 12345.678
              }
            },
            {
              "type": "Feature",
              "geometry": null,
              "properties": {
                "FIRSTNAME": "Grace",
                "LASTNAME": "Hopper",
                "AREA": 4321.5
              }
            }
          ]
        }
        """;
}
