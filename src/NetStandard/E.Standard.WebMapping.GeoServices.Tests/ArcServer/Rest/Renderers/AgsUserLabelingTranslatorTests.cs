using System.Text.Json;

using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Renderer.UserLabels;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers.UserLabels;

namespace E.Standard.WebMapping.GeoServices.Tests.ArcServer.Rest.Renderers;

public class AgsUserLabelingTranslatorTests
{
    public AgsUserLabelingTranslatorTests()
    {
        // no graphics engine in tests
        E.Standard.Platform.SystemInfo.DefaultFontName = "Arial";
    }

    [Fact]
    public void ToAgsLabelExpression_TextFieldsAndNewLine()
    {
        Assert.Equal(
            "\"Fläche: \" CONCAT [FLAECHE] CONCAT \" m²\" CONCAT NEWLINE CONCAT [NAME]",
            AgsUserLabelingTranslator.ToAgsLabelExpression("Fläche: [FLAECHE] m²\n[NAME]"));
    }

    [Fact]
    public void ToAgsLabelExpression_SingleField()
    {
        Assert.Equal("[NAME]", AgsUserLabelingTranslator.ToAgsLabelExpression("[NAME]"));
    }

    [Fact]
    public void ToAgsLabelExpression_QuotesAreRemoved()
    {
        Assert.Equal("\"a CONCAT b\"", AgsUserLabelingTranslator.ToAgsLabelExpression("a\" CONCAT \"b"));
    }

    [Fact]
    public void ToAgsLabelExpression_Empty_ReturnsNull()
    {
        Assert.Null(AgsUserLabelingTranslator.ToAgsLabelExpression(""));
    }

    [Fact]
    public void ToAgsLabelExpression_EncodesText()
    {
        Assert.Equal("\"a%26b\" CONCAT [NAME]", AgsUserLabelingTranslator.ToAgsLabelExpression("a&b[NAME]", s => s.Replace("&", "%26")));
    }

    [Fact]
    public void ShowLabels_DependsOnMode()
    {
        Assert.Null(AgsUserLabelingTranslator.ShowLabels(null));
        Assert.False(AgsUserLabelingTranslator.ShowLabels(new UserLabeling() { Mode = UserLabeling.ModeNone }));
        Assert.True(AgsUserLabelingTranslator.ShowLabels(new UserLabeling() { Mode = UserLabeling.ModeCustom }));
    }

    [Fact]
    public void ToAgsLabelingInfo_None_ReturnsNull()
    {
        Assert.Null(AgsUserLabelingTranslator.ToAgsLabelingInfo(new UserLabeling() { Mode = UserLabeling.ModeNone }, LayerType.point, 0, 0, null));
    }

    [Fact]
    public void ToAgsLabelingInfo_UnsupportedGeometry_ReturnsNull()
    {
        var labeling = new UserLabeling() { Mode = UserLabeling.ModeCustom, Expression = "[NAME]" };

        Assert.Null(AgsUserLabelingTranslator.ToAgsLabelingInfo(labeling, LayerType.unknown, 0, 0, null));
    }

    [Fact]
    public void ToAgsLabelingInfo_Custom_CreatesLabelingInfo()
    {
        var labeling = new UserLabeling()
        {
            Mode = UserLabeling.ModeCustom,
            Expression = "[NAME]",
            FontSize = 12,
            FontStyle = UserLabeling.FontStyleBoldItalic,
            FontColor = "#ff0000",
            HaloColor = "#ffffff"
        };

        var info = AgsUserLabelingTranslator.ToAgsLabelingInfo(labeling, LayerType.polygon, 1000, 50000, "A=1")!;

        Assert.Equal("esriServerPolygonPlacementAlwaysHorizontal", info.LabelPlacement);
        Assert.Equal("[NAME]", info.LabelExpression);
        Assert.True(info.UseCodedValues);
        Assert.Equal(50000, info.MinScale);
        Assert.Equal(1000, info.MaxScale);
        Assert.Equal("A=1", info.Where);

        using var symbol = JsonDocument.Parse(JsonSerializer.Serialize(info.Symbol));
        var root = symbol.RootElement;
        Assert.Equal("esriTS", root.GetProperty("type").GetString());
        Assert.Equal([255, 0, 0, 255], root.GetProperty("color").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal([255, 255, 255, 255], root.GetProperty("haloColor").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(AgsUserLabelingTranslator.HaloSize, root.GetProperty("haloSize").GetInt32());
        Assert.Equal(12, root.GetProperty("font").GetProperty("size").GetInt32());
        Assert.Equal("italic", root.GetProperty("font").GetProperty("style").GetString());
        Assert.Equal("bold", root.GetProperty("font").GetProperty("weight").GetString());
    }

    [Fact]
    public void ToAgsLabelingInfo_Defaults_NoHalo()
    {
        var labeling = new UserLabeling() { Mode = UserLabeling.ModeCustom, Expression = "[NAME]" };

        var info = AgsUserLabelingTranslator.ToAgsLabelingInfo(labeling, LayerType.point, 0, 0, "")!;

        Assert.Null(info.Where);

        using var symbol = JsonDocument.Parse(JsonSerializer.Serialize(info.Symbol));
        var root = symbol.RootElement;
        Assert.Equal([0, 0, 0, 255], root.GetProperty("color").EnumerateArray().Select(e => e.GetInt32()));
        Assert.False(root.TryGetProperty("haloColor", out _));
        Assert.Equal((int)AgsUserLabelingTranslator.DefaultFontSize, root.GetProperty("font").GetProperty("size").GetInt32());
        Assert.Equal("normal", root.GetProperty("font").GetProperty("weight").GetString());
    }
}
