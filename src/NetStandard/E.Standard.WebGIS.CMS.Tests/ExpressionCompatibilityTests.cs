using E.Standard.CMS.Core;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.CMS.Tests;

public class ExpressionCompatibilityTests
{
    [Fact]
    public void SolveExpression_PreservesFieldPlaceholderBehavior()
    {
        var feature = CreateFeature();

        Assert.Equal("Main/Main", Globals.SolveExpression(feature, "[NAME]/[NAME]"));
        Assert.Equal("Main", Globals.SolveExpression(feature, "[!NAME]"));
        Assert.Equal(String.Empty, Globals.SolveExpression(feature, "[!EMPTY]"));
        Assert.Equal("Main", Globals.SolveExpression(feature, "[~NAME]"));
        Assert.Equal(12.345.ToString("0.00"), Globals.SolveExpression(feature, "[NUMBER:0.00]"));
        Assert.Equal("A%20B%23", Globals.SolveExpression(feature, "[url-encode:URL]"));
        Assert.Equal("M%fcller", Globals.SolveExpression(feature, "[url-encode-latin1:UMLAUT]"));
        Assert.Equal("[NAME]", Globals.SolveExpression(null!, "[NAME]"));
        Assert.Equal("plain", Globals.SolveExpression(feature, "plain"));
    }

    [Fact]
    public void SolveExpression_WithPreparsedParameters_MatchesDefaultOverload()
    {
        const string expression = "[NAME]/[NUMBER:0.00]/[url-encode:URL]";
        var feature = CreateFeature();
        var parameters = Helper.GetKeyParameters(expression);

        Assert.Equal(
            Globals.SolveExpression(feature, expression),
            Globals.SolveExpression(feature, expression, parameters));
    }

    [Fact]
    public void SolveExpression_PreservesSpatialPlaceholderBehavior()
    {
        var feature = CreateFeature();

        Assert.Equal("10,20,10,20", Globals.SolveExpression(feature, "[BBOX]"));
        Assert.Equal("10,20,10,20", Globals.SolveExpression(feature, "[spatial::bbox::4326]"));
        Assert.Equal("10,20", Globals.SolveExpression(feature, "[spatial::point]"));
        Assert.Equal("10,20", Globals.SolveExpression(feature, "[spatial::point::4326]"));
        Assert.Equal("20,10", Globals.SolveExpression(feature, "[spatial::latlng]"));
        Assert.Equal("10,20", Globals.SolveExpression(feature, "[spatial::lnglat]"));
        Assert.Equal("10", Globals.SolveExpression(feature, "[spatial::lng]"));
        Assert.Equal("20", Globals.SolveExpression(feature, "[spatial::lat]"));
    }

    [Fact]
    public void Helper_PreservesKeyScanningAndFieldExtraction()
    {
        Assert.Equal(
            ["NAME", "NAME", "!REQUIRED", "NUMBER:0.0", "spatial::point", "url-encode:URL"],
            Helper.GetKeyParameters("[NAME]-[NAME]-[!REQUIRED]-[NUMBER:0.0]-[spatial::point]-[url-encode:URL]"));
        Assert.Equal(
            ["NAME", "REQUIRED", "NUMBER", "URL"],
            Helper.GetKeyParameterFields("[NAME]-[!REQUIRED]-[NUMBER:0.0]-[spatial::point]-[url-encode:URL]"));
        Assert.Null(Helper.GetKeyParameters("plain"));
        Assert.Null(Helper.GetKeyParameters("[unfinished"));
        Assert.Null(Helper.GetKeyParameters("[]"));
        Assert.Equal(["A", ""], Helper.GetKeyParameters("[A][]"));
        Assert.Equal(["A"], Helper.GetKeyParameters("[][A]"));
        Assert.Equal(["A", "", "B"], Helper.GetKeyParameters("[A][][B]"));
        Assert.Equal(["A", "B"], Helper.GetKeyParameters("[A;B]"));
    }

    private static Feature CreateFeature()
        => new(
        [
            new WebMapping.Core.Attribute("NAME", "Main"),
            new WebMapping.Core.Attribute("EMPTY", String.Empty),
            new WebMapping.Core.Attribute("NUMBER", "12.345"),
            new WebMapping.Core.Attribute("URL", "A B#"),
            new WebMapping.Core.Attribute("UMLAUT", "Müller")
        ])
        {
            Shape = new Point(10, 20)
            {
                SrsId = 4326
            }
        };
}
