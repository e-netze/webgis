using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Renderer.UserRenderers;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers.UserRenderers;

namespace E.Standard.WebMapping.GeoServices.Tests.ArcServer.Rest.Renderers;

public class AgsUserRendererTranslatorTests
{
    [Fact]
    public void ToAgsRenderer_Null_ReturnsNull()
    {
        Assert.Null(AgsUserRendererTranslator.ToAgsRenderer(null, LayerType.point));
    }

    [Fact]
    public void ToAgsRenderer_UnsupportedGeometry_ReturnsNull()
    {
        var renderer = new UserRenderer() { Type = UserRenderer.TypeSimple, Symbol = new UserSymbol() };

        Assert.Null(AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.unknown));
    }

    [Fact]
    public void ToAgsRenderer_UnknownType_ReturnsNull()
    {
        var renderer = new UserRenderer() { Type = "classBreaks", Symbol = new UserSymbol() };

        Assert.Null(AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.point));
    }

    [Fact]
    public void ToAgsRenderer_SimplePoint_CreatesMarkerSymbol()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeSimple,
            Symbol = new UserSymbol() { Color = "#ff0000", Opacity = 0.5f, Marker = "square", Size = 12, OutlineColor = "#00ff00", OutlineWidth = 2 }
        };

        var ags = AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.point)!;

        Assert.Equal("simple", ags["type"]);
        var symbol = (Dictionary<string, object>)ags["symbol"];
        Assert.Equal("esriSMS", symbol["type"]);
        Assert.Equal("esriSMSSquare", symbol["style"]);
        Assert.Equal(new[] { 255, 0, 0, 128 }, (int[])symbol["color"]);
        Assert.Equal(12f, Convert.ToSingle(symbol["size"]));

        var outline = (Dictionary<string, object>)symbol["outline"];
        Assert.Equal(new[] { 0, 255, 0, 255 }, (int[])outline["color"]);
        Assert.Equal(2f, Convert.ToSingle(outline["width"]));
    }

    [Fact]
    public void ToAgsRenderer_SimpleLine_CreatesLineSymbol()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeSimple,
            Symbol = new UserSymbol() { Color = "#0000ff", Width = 3, LineStyle = "dash" }
        };

        var symbol = (Dictionary<string, object>)AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.line)!["symbol"];

        Assert.Equal("esriSLS", symbol["type"]);
        Assert.Equal("esriSLSDash", symbol["style"]);
        Assert.Equal(new[] { 0, 0, 255, 255 }, (int[])symbol["color"]);
        Assert.Equal(3f, Convert.ToSingle(symbol["width"]));
    }

    [Fact]
    public void ToAgsRenderer_SimplePolygon_CreatesFillSymbol()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeSimple,
            Symbol = new UserSymbol() { Color = "#ffffff", FillStyle = "backwarddiagonal", OutlineColor = "#000000", OutlineWidth = 1, OutlineStyle = "dot" }
        };

        var symbol = (Dictionary<string, object>)AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.polygon)!["symbol"];

        Assert.Equal("esriSFS", symbol["type"]);
        Assert.Equal("esriSFSBackwardDiagonal", symbol["style"]);

        var outline = (Dictionary<string, object>)symbol["outline"];
        Assert.Equal("esriSLS", outline["type"]);
        Assert.Equal("esriSLSDot", outline["style"]);
    }

    [Fact]
    public void ToAgsRenderer_PolygonWithoutOutline_UsesNullOutlineStyle()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeSimple,
            Symbol = new UserSymbol() { Color = "#ffffff", FillStyle = "none", OutlineWidth = 0 }
        };

        var symbol = (Dictionary<string, object>)AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.polygon)!["symbol"];

        Assert.Equal("esriSFSNull", symbol["style"]);
        Assert.Equal("esriSLSNull", ((Dictionary<string, object>)symbol["outline"])["style"]);
    }

    [Fact]
    public void ToAgsRenderer_UniqueValueWithDefaultSymbol_CreatesUniqueValueRenderer()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeUniqueValue,
            Field = "TYP",
            Items =
            [
                new UserRendererItem() { Value = "A", Label = "Typ A", Symbol = new UserSymbol() { Color = "#ff0000" } },
                new UserRendererItem() { Value = "B", Symbol = new UserSymbol() { Color = "#00ff00" } }
            ],
            DefaultSymbol = new UserSymbol() { Color = "#cccccc" },
            DefaultLabel = "Sonstige"
        };

        var ags = AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.line)!;

        Assert.Equal("uniqueValue", ags["type"]);
        Assert.Equal("TYP", ags["field1"]);

        var infos = (Dictionary<string, object>[])ags["uniqueValueInfos"];
        Assert.Equal(2, infos.Length);
        Assert.Equal("A", infos[0]["value"]);
        Assert.Equal("Typ A", infos[0]["label"]);
        Assert.Equal("B", infos[1]["label"]);  // label falls back to value

        Assert.True(ags.ContainsKey("defaultSymbol"));
        Assert.Equal("Sonstige", ags["defaultLabel"]);
    }

    [Fact]
    public void ToAgsRenderer_UniqueValueWithoutDefaultSymbol_OmitsDefault()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeUniqueValue,
            Field = "TYP",
            Items = [new UserRendererItem() { Value = "A", Symbol = new UserSymbol() }]
        };

        var ags = AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.point)!;

        Assert.False(ags.ContainsKey("defaultSymbol"));
        Assert.False(ags.ContainsKey("defaultLabel"));
    }

    [Fact]
    public void ToAgsRenderer_EncodeString_IsAppliedToUserStrings()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeUniqueValue,
            Field = "TYP",
            Items = [new UserRendererItem() { Value = "A&B", Label = "A+B", Symbol = new UserSymbol() }]
        };

        var ags = AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.point, s => s.Replace("&", "%26").Replace("+", "%2B"))!;
        var infos = (Dictionary<string, object>[])ags["uniqueValueInfos"];

        Assert.Equal("A%26B", infos[0]["value"]);
        Assert.Equal("A%2BB", infos[0]["label"]);
    }

    [Fact]
    public void ToAgsRenderer_ClassBreaks_TranslatesClasses()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeClassBreaks,
            Field = "AREA",
            MinValue = 0,
            ClassBreaks =
            [
                new UserRendererClassBreak() { MaxValue = 10.5, Symbol = new UserSymbol() { Color = "#ff0000" } },
                new UserRendererClassBreak() { MaxValue = 100, Label = "big", Symbol = new UserSymbol() { Color = "#00ff00" } }
            ],
            DefaultSymbol = new UserSymbol(),
            DefaultLabel = "Other"
        };

        var ags = AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.polygon)!;
        var infos = (Dictionary<string, object>[])ags["classBreakInfos"];

        Assert.Equal("classBreaks", ags["type"]);
        Assert.Equal("AREA", ags["field"]);
        Assert.Equal(0d, ags["minValue"]);
        Assert.Equal(2, infos.Length);
        Assert.Equal(0d, infos[0]["classMinValue"]);
        Assert.Equal(10.5, infos[0]["classMaxValue"]);
        Assert.Equal("0 - 10.5", infos[0]["label"]);
        Assert.Equal(10.5, infos[1]["classMinValue"]);
        Assert.Equal(100d, infos[1]["classMaxValue"]);
        Assert.Equal("big", infos[1]["label"]);
        Assert.Equal("Other", ags["defaultLabel"]);
    }

    [Fact]
    public void ToAgsRenderer_ClassBreaks_WithoutMinValue_UsesFirstBreak()
    {
        var renderer = new UserRenderer()
        {
            Type = UserRenderer.TypeClassBreaks,
            Field = "AREA",
            ClassBreaks = [new UserRendererClassBreak() { MaxValue = 5, Symbol = new UserSymbol() }]
        };

        var ags = AgsUserRendererTranslator.ToAgsRenderer(renderer, LayerType.point)!;

        Assert.Equal(5d, ags["minValue"]);
        Assert.False(ags.ContainsKey("defaultSymbol"));
    }

    [Theory]
    [InlineData("#000000", 1f, new[] { 0, 0, 0, 255 })]
    [InlineData("#FFFFFF", 0f, new[] { 255, 255, 255, 0 })]
    [InlineData("#1a2b3c", 1f, new[] { 26, 43, 60, 255 })]
    [InlineData("invalid", 1f, new[] { 128, 128, 128, 255 })]
    public void ToAgsColor_ConvertsHexAndOpacity(string hex, float opacity, int[] expected)
    {
        Assert.Equal(expected, AgsUserRendererTranslator.ToAgsColor(hex, opacity));
    }
}
