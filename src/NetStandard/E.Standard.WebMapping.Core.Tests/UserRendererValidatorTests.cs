using E.Standard.WebMapping.Core.Renderer.UserRenderers;

namespace E.Standard.WebMapping.Core.Tests;

public class UserRendererValidatorTests
{
    private static readonly string[] AllowedFields = ["TYPE", "NAME"];

    private static UserSymbol PointSymbol() => new() { Color = "#ff0000", Marker = "circle", Size = 8, Opacity = 0.5f, OutlineColor = "#000000", OutlineWidth = 1 };
    private static UserSymbol LineSymbol() => new() { Color = "#00ff00", Width = 2, LineStyle = "dash" };
    private static UserSymbol PolygonSymbol() => new() { Color = "#0000ff", Opacity = 0.3f, FillStyle = "solid", OutlineColor = "#000000", OutlineWidth = 1, OutlineStyle = "solid" };

    private static UserRenderer Simple(UserSymbol symbol) => new() { Type = UserRenderer.TypeSimple, Symbol = symbol };

    private static UserRenderer UniqueValue(string field, int count) => new()
    {
        Type = UserRenderer.TypeUniqueValue,
        Field = field,
        Items = Enumerable.Range(0, count)
                          .Select(i => new UserRendererItem() { Value = i.ToString(), Label = $"Value {i}", Symbol = PolygonSymbol() })
                          .ToList(),
        DefaultSymbol = PolygonSymbol(),
        DefaultLabel = "Other"
    };

    [Theory]
    [InlineData(LayerType.point)]
    [InlineData(LayerType.line)]
    [InlineData(LayerType.polygon)]
    public void Validate_SimpleRenderer_IsValid(LayerType geometryType)
    {
        var symbol = geometryType switch
        {
            LayerType.point => PointSymbol(),
            LayerType.line => LineSymbol(),
            _ => PolygonSymbol()
        };

        var errors = new UserRendererValidator().Validate(Simple(symbol), geometryType, null);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(LayerType.image)]
    [InlineData(LayerType.annotation)]
    [InlineData(LayerType.unknown)]
    public void Validate_UnsupportedGeometryType_IsInvalid(LayerType geometryType)
    {
        Assert.False(new UserRendererValidator().IsValid(Simple(PolygonSymbol()), geometryType, null));
    }

    [Fact]
    public void Validate_Null_IsInvalid()
    {
        Assert.False(new UserRendererValidator().IsValid(null, LayerType.polygon, null));
    }

    [Fact]
    public void Validate_UnsupportedVersion_IsInvalid()
    {
        var renderer = Simple(PolygonSymbol());
        renderer.Version = 99;

        Assert.False(new UserRendererValidator().IsValid(renderer, LayerType.polygon, null));
    }

    [Fact]
    public void Validate_UnsupportedType_IsInvalid()
    {
        var renderer = Simple(PolygonSymbol());
        renderer.Type = "classBreaks";

        Assert.False(new UserRendererValidator().IsValid(renderer, LayerType.polygon, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("red")]
    [InlineData("#ff00")]
    [InlineData("#ff0000ff")]
    [InlineData("#gg0000")]
    public void Validate_InvalidColor_IsInvalid(string? color)
    {
        var symbol = PolygonSymbol();
        symbol.Color = color;

        Assert.False(new UserRendererValidator().IsValid(Simple(symbol), LayerType.polygon, null));
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(65f)]
    [InlineData(float.NaN)]
    public void Validate_PointSizeOutOfRange_IsInvalid(float size)
    {
        var symbol = PointSymbol();
        symbol.Size = size;

        Assert.False(new UserRendererValidator().IsValid(Simple(symbol), LayerType.point, null));
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void Validate_OpacityOutOfRange_IsInvalid(float opacity)
    {
        var symbol = PolygonSymbol();
        symbol.Opacity = opacity;

        Assert.False(new UserRendererValidator().IsValid(Simple(symbol), LayerType.polygon, null));
    }

    [Fact]
    public void Validate_LineWidthOutOfRange_IsInvalid()
    {
        var symbol = LineSymbol();
        symbol.Width = 21;

        Assert.False(new UserRendererValidator().IsValid(Simple(symbol), LayerType.line, null));
    }

    [Fact]
    public void Validate_UnknownMarker_IsInvalid()
    {
        var symbol = PointSymbol();
        symbol.Marker = "star";

        Assert.False(new UserRendererValidator().IsValid(Simple(symbol), LayerType.point, null));
    }

    [Fact]
    public void Validate_UnknownFillStyle_IsInvalid()
    {
        var symbol = PolygonSymbol();
        symbol.FillStyle = "dots";

        Assert.False(new UserRendererValidator().IsValid(Simple(symbol), LayerType.polygon, null));
    }

    [Fact]
    public void Validate_SimpleRendererWithoutSymbol_IsInvalid()
    {
        Assert.False(new UserRendererValidator().IsValid(new UserRenderer() { Type = UserRenderer.TypeSimple }, LayerType.polygon, null));
    }

    [Fact]
    public void Validate_UniqueValue_IsValid()
    {
        Assert.Empty(new UserRendererValidator().Validate(UniqueValue("TYPE", 5), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_UniqueValue_FieldIsCaseInsensitive()
    {
        Assert.True(new UserRendererValidator().IsValid(UniqueValue("type", 5), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_UniqueValue_FieldNotAllowed_IsInvalid()
    {
        Assert.False(new UserRendererValidator().IsValid(UniqueValue("SECRET", 5), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_UniqueValue_NoAllowedFields_IsInvalid()
    {
        Assert.False(new UserRendererValidator().IsValid(UniqueValue("TYPE", 5), LayerType.polygon, null));
    }

    [Fact]
    public void Validate_UniqueValue_TooManyClasses_IsInvalid()
    {
        var validator = new UserRendererValidator(maxUniqueValueClasses: 10);

        Assert.True(validator.IsValid(UniqueValue("TYPE", 10), LayerType.polygon, AllowedFields));
        Assert.False(validator.IsValid(UniqueValue("TYPE", 11), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_UniqueValue_DuplicateValues_IsInvalid()
    {
        var renderer = UniqueValue("TYPE", 2);
        renderer.Items![1].Value = renderer.Items[0].Value;

        Assert.False(new UserRendererValidator().IsValid(renderer, LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_UniqueValue_InvalidItemSymbol_IsInvalid()
    {
        var renderer = UniqueValue("TYPE", 2);
        renderer.Items![1].Symbol!.Color = "blue";

        Assert.False(new UserRendererValidator().IsValid(renderer, LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_UniqueValue_WithoutItemsAndDefaultSymbol_IsInvalid()
    {
        var renderer = new UserRenderer() { Type = UserRenderer.TypeUniqueValue, Field = "TYPE" };

        Assert.False(new UserRendererValidator().IsValid(renderer, LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_UniqueValue_WithoutDefaultSymbol_IsValid()
    {
        var renderer = UniqueValue("TYPE", 2);
        renderer.DefaultSymbol = null;

        Assert.True(new UserRendererValidator().IsValid(renderer, LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_UniqueValue_LabelTooLong_IsInvalid()
    {
        var renderer = UniqueValue("TYPE", 1);
        renderer.Items![0].Label = new string('x', UserRendererValidator.MaxTextLength + 1);

        Assert.False(new UserRendererValidator().IsValid(renderer, LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Deserialize_SystemTextJson_ReadsClientJson()
    {
        var json = """
            {
              "version": 1,
              "type": "uniqueValue",
              "field": "TYPE",
              "items": [ { "value": "A", "label": "Type A", "symbol": { "color": "#ff0000", "opacity": 0.5, "fillStyle": "solid", "outlineColor": "#000000", "outlineWidth": 1 } } ],
              "defaultSymbol": { "color": "#cccccc" },
              "defaultLabel": "Other"
            }
            """;

        var renderer = System.Text.Json.JsonSerializer.Deserialize<UserRenderer>(json);

        Assert.NotNull(renderer);
        Assert.Equal(UserRenderer.TypeUniqueValue, renderer.Type);
        Assert.Equal("TYPE", renderer.Field);
        Assert.Single(renderer.Items!);
        Assert.Equal("Type A", renderer.Items![0].Label);
        Assert.Equal(0.5f, renderer.Items[0].Symbol!.Opacity);
        Assert.Equal("#cccccc", renderer.DefaultSymbol!.Color);
        Assert.True(new UserRendererValidator().IsValid(renderer, LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Serialize_SystemTextJson_RoundTripsWithNewtonsoft()
    {
        var renderer = UniqueValue("TYPE", 2);

        var json = System.Text.Json.JsonSerializer.Serialize(renderer);
        var roundTripped = Newtonsoft.Json.JsonConvert.DeserializeObject<UserRenderer>(json);

        Assert.DoesNotContain("null", json);
        Assert.Equal(json, System.Text.Json.JsonSerializer.Serialize(roundTripped));
    }

    private static UserRenderer ClassBreaks(string field, params double[] breaks) => new()
    {
        Type = UserRenderer.TypeClassBreaks,
        Field = field,
        MinValue = 0,
        ClassBreaks = breaks.Select(b => new UserRendererClassBreak() { MaxValue = b, Label = $"<= {b}", Symbol = PolygonSymbol() }).ToList()
    };

    [Fact]
    public void Validate_ClassBreaks_IsValid()
    {
        Assert.Empty(new UserRendererValidator().Validate(ClassBreaks("TYPE", 10, 20, 30), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_ClassBreaks_NotAscending_IsInvalid()
    {
        Assert.False(new UserRendererValidator().IsValid(ClassBreaks("TYPE", 10, 5), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_ClassBreaks_MinValueGreaterThanFirstBreak_IsInvalid()
    {
        var renderer = ClassBreaks("TYPE", 10);
        renderer.MinValue = 20;

        Assert.False(new UserRendererValidator().IsValid(renderer, LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_ClassBreaks_NoClasses_IsInvalid()
    {
        Assert.False(new UserRendererValidator().IsValid(ClassBreaks("TYPE"), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_ClassBreaks_NotAllowedField_IsInvalid()
    {
        Assert.False(new UserRendererValidator().IsValid(ClassBreaks("SECRET", 10), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_ClassBreaks_NaN_IsInvalid()
    {
        Assert.False(new UserRendererValidator().IsValid(ClassBreaks("TYPE", double.NaN), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Validate_ClassBreaks_TooManyClasses_IsInvalid()
    {
        var validator = new UserRendererValidator(maxUniqueValueClasses: 2);

        Assert.False(validator.IsValid(ClassBreaks("TYPE", 1, 2, 3), LayerType.polygon, AllowedFields));
    }

    [Fact]
    public void Deserialize_ClassBreaks_ReadsClientJson()
    {
        var json = """
            { "version": 1, "type": "classBreaks", "field": "TYPE", "minValue": 0,
              "classBreaks": [ { "maxValue": 10, "label": "low", "symbol": { "color": "#ff0000" } },
                               { "maxValue": 20.5, "symbol": { "color": "#00ff00" } } ] }
            """;

        var renderer = System.Text.Json.JsonSerializer.Deserialize<UserRenderer>(json);

        Assert.NotNull(renderer);
        Assert.Equal(2, renderer.ClassBreaks!.Count);
        Assert.Equal(20.5, renderer.ClassBreaks[1].MaxValue);
        Assert.True(new UserRendererValidator().IsValid(renderer, LayerType.polygon, AllowedFields));
    }

    [Theory]
    [InlineData(FieldType.Interger, true)]
    [InlineData(FieldType.Double, true)]
    [InlineData(FieldType.String, false)]
    [InlineData(FieldType.Date, false)]
    public void IsNumericFieldType(FieldType fieldType, bool expected)
    {
        Assert.Equal(expected, UserRendererValidator.IsNumericFieldType(fieldType));
    }
}
