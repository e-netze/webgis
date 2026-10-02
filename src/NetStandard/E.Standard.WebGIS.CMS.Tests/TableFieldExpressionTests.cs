using System.Collections.Specialized;

using E.Standard.Api.App.Data;
using E.Standard.Api.App.DTOs;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.CMS.Tests;

public class TableFieldExpressionTests
{
    [Fact]
    public async Task RenderField_PreservesLegacyTemplateAndEvalPipeline()
    {
        var field = new TableFieldExpression
        {
            Expression = "[NAME]: $round2([VALUE])"
        };

        var result = await RenderAsync(field);

        Assert.Equal($"Main: {Math.Round(12.345, 2).ToString("0.00")}", result);
    }

    [Fact]
    public async Task RenderField_DoesNotEvaluateExpressionProvidedByFieldValue()
    {
        var field = new TableFieldExpression
        {
            Expression = "[DYNAMIC]"
        };
        var feature = new Feature(
        [
            new WebMapping.Core.Attribute("DYNAMIC", "$round2(12.345)")
        ]);
        var context = await CreateContextAsync(field);
        var result = context.RenderField(field, feature);

        Assert.Equal("$round2(12.345)", result);
    }

    [Fact]
    public async Task StructuredExpression_DoesNotEvaluateExpressionProvidedByFieldValue()
    {
        var field = new TableFieldExpression
        {
            Expression = "concat(\"Value: \", [DYNAMIC])"
        };
        var feature = new Feature(
        [
            new WebMapping.Core.Attribute(
                "DYNAMIC",
                "round(12.345, 2) || unknown()")
        ]);
        var context = await CreateContextAsync(field);
        var result = context.RenderField(field, feature);

        Assert.Equal("Value: round(12.345, 2) || unknown()", result);
    }

    [Fact]
    public async Task RenderField_EvaluatesStructuredExpressionWithoutPrefix()
    {
        var field = new TableFieldExpression
        {
            Expression = "concat([NAME], \": \", round([VALUE] * 2, 1))"
        };

        var result = await RenderAsync(field);

        Assert.Equal("Main: 24.7", result);
    }

    [Fact]
    public async Task RenderField_EvaluatesOperatorExpressionWithoutPrefix()
    {
        var field = new TableFieldExpression
        {
            Expression = "[VALUE] + 1"
        };

        var result = await RenderAsync(field);

        Assert.Equal("13.345", result);
    }

    [Fact]
    public async Task RenderField_EvaluatesShapeArea()
    {
        var field = new TableFieldExpression
        {
            Expression = "round(shape_area(), 2)"
        };
        var feature = CreateFeature();
        feature.Shape = new Polygon(
        [
            new Ring(
            [
                new Point(0, 0),
                new Point(10, 0),
                new Point(10, 5),
                new Point(0, 5),
                new Point(0, 0)
            ])
        ]);
        var context = await CreateContextAsync(field);

        var result = context.RenderField(field, feature);

        Assert.Equal("50", result);
    }

    [Fact]
    public async Task RenderField_EvaluatesShapeFunctionsWithMatchingSRefId()
    {
        var field = new TableFieldExpression
        {
            Expression = "concat(shape_len(4326), \";\", shape_centroid_x(4326))"
        };
        var feature = CreateFeature();
        feature.Shape = new Polyline(
        [
            new Point(0, 0),
            new Point(6, 0),
            new Point(6, 8)
        ])
        {
            SrsId = 4326
        };
        var context = await CreateContextAsync(field);

        var result = context.RenderField(field, feature);

        Assert.Equal("14;6", result);
    }

    [Fact]
    public async Task RenderField_ReturnsNullForShapeAreaWithoutPolygon()
    {
        var field = new TableFieldExpression
        {
            Expression = "coalesce(shape_area(), \"\")"
        };

        var result = await RenderAsync(field);

        Assert.Equal(String.Empty, result);
    }

    [Fact]
    public async Task RenderField_DoesNotTreatLeadingEqualsAsTableExpressionPrefix()
    {
        var field = new TableFieldExpression
        {
            Expression = "=concat([NAME], \"!\")"
        };

        var result = await RenderAsync(field);

        Assert.Equal("=concat(Main, \"!\")", result);
    }

    [Fact]
    public void FeatureFieldNames_StillReportsStructuredExpressionFields()
    {
        var field = new TableFieldExpression
        {
            Expression = "concat([NAME], [MISSING])"
        };

        Assert.Equal(["NAME", "MISSING"], field.FeatureFieldNames);
    }

    [Fact]
    public async Task RenderingContext_RejectsUnknownField()
    {
        var field = new TableFieldExpression
        {
            Expression = "[VALUE] + 1"
        };
        var context = await CreateContextAsync(field);
        var otherField = new TableFieldExpression
        {
            Expression = "[VALUE] + 2"
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => context.GetFieldContext(otherField));

        Assert.Contains("No rendering context", exception.Message);
    }

    [Fact]
    public async Task RenderingContexts_KeepPreparedExpressionsIndependent()
    {
        var field = new TableFieldExpression
        {
            Expression = "[VALUE] + 1"
        };
        var firstContext = await CreateContextAsync(field);
        field.Expression = "[VALUE] + 2";
        var secondContext = await CreateContextAsync(field);

        Assert.Equal(
            "13.345",
            firstContext.RenderField(field, CreateFeature()));
        Assert.Equal(
            "14.345",
            secondContext.RenderField(field, CreateFeature()));
    }

    private static async Task<string> RenderAsync(TableFieldExpression field)
    {
        var context = await CreateContextAsync(field);
        return context.RenderField(field, CreateFeature());
    }

    private static Task<QueryFieldRenderingContext> CreateContextAsync(
        params TableField[] fields)
        => new QueryDTO
        {
            Fields = fields
        }.InitFieldRendering(
            httpService: null!,
            requestHeaders: new NameValueCollection());

    private static Feature CreateFeature()
        => new(
        [
            new WebMapping.Core.Attribute("NAME", "Main"),
            new WebMapping.Core.Attribute("VALUE", "12.345")
        ]);
}
