using E.Standard.WebMapping.Core.Renderer.UserLabels;

namespace E.Standard.WebMapping.Core.Tests;

public class UserLabelingValidatorTests
{
    private static readonly string[] AllowedFields = ["FLAECHE", "NAME"];

    private static UserLabeling Custom(string expression) => new()
    {
        Mode = UserLabeling.ModeCustom,
        Expression = expression,
        FontSize = 10,
        FontStyle = UserLabeling.FontStyleBold,
        FontColor = "#000000",
        HaloColor = "#ffffff"
    };

    [Fact]
    public void Parse_TextFieldsAndNewLines()
    {
        var tokens = UserLabelExpression.Parse("Fläche: [FLAECHE] m²\r\n[ NAME ]");

        Assert.Equal(
            [
                new UserLabelExpressionToken(UserLabelExpressionTokenType.Text, "Fläche: "),
                new UserLabelExpressionToken(UserLabelExpressionTokenType.Field, "FLAECHE"),
                new UserLabelExpressionToken(UserLabelExpressionTokenType.Text, " m²"),
                new UserLabelExpressionToken(UserLabelExpressionTokenType.NewLine, "\n"),
                new UserLabelExpressionToken(UserLabelExpressionTokenType.Field, "NAME"),
            ],
            tokens);
    }

    [Fact]
    public void Parse_RemovesDoubleQuotes()
    {
        var tokens = UserLabelExpression.Parse("a\"b\" CONCAT [NAME]");

        Assert.Equal("ab CONCAT ", tokens[0].Value);
    }

    [Fact]
    public void Parse_UnclosedBracket_IsText()
    {
        var tokens = UserLabelExpression.Parse("[NAME");

        Assert.Single(tokens);
        Assert.Equal(UserLabelExpressionTokenType.Text, tokens[0].Type);
    }

    [Theory]
    [InlineData(LayerType.point)]
    [InlineData(LayerType.line)]
    [InlineData(LayerType.polygon)]
    public void Validate_Custom_IsValid(LayerType geometryType)
    {
        Assert.Empty(new UserLabelingValidator().Validate(Custom("Fläche: [flaeche] m²\n[NAME]"), geometryType, AllowedFields));
    }

    [Fact]
    public void Validate_None_IsValid()
    {
        Assert.Empty(new UserLabelingValidator().Validate(new UserLabeling() { Mode = UserLabeling.ModeNone }, LayerType.point, null));
    }

    [Fact]
    public void Validate_TextOnly_IsValid()
    {
        Assert.Empty(new UserLabelingValidator().Validate(Custom("Hallo"), LayerType.point, AllowedFields));
    }

    [Fact]
    public void Validate_Null_IsInvalid()
    {
        Assert.NotEmpty(new UserLabelingValidator().Validate(null, LayerType.point, AllowedFields));
    }

    [Fact]
    public void Validate_UnsupportedGeometry_IsInvalid()
    {
        Assert.NotEmpty(new UserLabelingValidator().Validate(Custom("[NAME]"), LayerType.unknown, AllowedFields));
    }

    [Fact]
    public void Validate_UnknownMode_IsInvalid()
    {
        var labeling = Custom("[NAME]");
        labeling.Mode = "something";

        Assert.NotEmpty(new UserLabelingValidator().Validate(labeling, LayerType.point, AllowedFields));
    }

    [Fact]
    public void Validate_WrongVersion_IsInvalid()
    {
        var labeling = Custom("[NAME]");
        labeling.Version = 99;

        Assert.NotEmpty(new UserLabelingValidator().Validate(labeling, LayerType.point, AllowedFields));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n")]
    public void Validate_EmptyExpression_IsInvalid(string? expression)
    {
        Assert.NotEmpty(new UserLabelingValidator().Validate(Custom(expression!), LayerType.point, AllowedFields));
    }

    [Fact]
    public void Validate_FieldNotAllowed_IsInvalid()
    {
        Assert.NotEmpty(new UserLabelingValidator().Validate(Custom("[SECRET]"), LayerType.point, AllowedFields));
    }

    [Fact]
    public void Validate_NoAllowedFields_FieldIsInvalid()
    {
        Assert.NotEmpty(new UserLabelingValidator().Validate(Custom("[NAME]"), LayerType.point, null));
    }

    [Fact]
    public void Validate_TooLong_IsInvalid()
    {
        Assert.NotEmpty(new UserLabelingValidator().Validate(Custom(new string('x', UserLabelingValidator.MaxExpressionLength + 1)), LayerType.point, AllowedFields));
    }

    [Fact]
    public void Validate_TooManyLines_IsInvalid()
    {
        Assert.NotEmpty(new UserLabelingValidator().Validate(Custom("a\nb\nc\nd"), LayerType.point, AllowedFields));
    }

    [Theory]
    [InlineData(2f)]
    [InlineData(100f)]
    [InlineData(float.NaN)]
    public void Validate_InvalidFontSize_IsInvalid(float fontSize)
    {
        var labeling = Custom("[NAME]");
        labeling.FontSize = fontSize;

        Assert.NotEmpty(new UserLabelingValidator().Validate(labeling, LayerType.point, AllowedFields));
    }

    [Fact]
    public void Validate_InvalidFontStyle_IsInvalid()
    {
        var labeling = Custom("[NAME]");
        labeling.FontStyle = "underline";

        Assert.NotEmpty(new UserLabelingValidator().Validate(labeling, LayerType.point, AllowedFields));
    }

    [Theory]
    [InlineData("red", null)]
    [InlineData(null, "#fff")]
    public void Validate_InvalidColor_IsInvalid(string? fontColor, string? haloColor)
    {
        var labeling = Custom("[NAME]");
        labeling.FontColor = fontColor;
        labeling.HaloColor = haloColor;

        Assert.NotEmpty(new UserLabelingValidator().Validate(labeling, LayerType.point, AllowedFields));
    }

    [Fact]
    public void Validate_OptionalPropertiesNull_IsValid()
    {
        var labeling = new UserLabeling() { Mode = UserLabeling.ModeCustom, Expression = "[NAME]" };

        Assert.Empty(new UserLabelingValidator().Validate(labeling, LayerType.point, AllowedFields));
    }
}
