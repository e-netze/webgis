using System.Globalization;

namespace E.Standard.WebMapping.Core.Tests;

[Collection("Culture-sensitive tests")]
public class EvalCompatibilityTests
{
    [Fact]
    public void ParseEvalExpression_PreservesLegacyFunctions()
    {
        using var culture = new CultureScope("en-US");

        Assert.Equal(Math.PI.ToString(), Eval.ParseEvalExpression("$pi()"));
        Assert.Equal("7", Eval.ParseEvalExpression("$eval(1+2*3)"));
        Assert.Equal("prefix 3 suffix", Eval.ParseEvalExpression("prefix $eval(1+2) suffix"));
        Assert.Equal("0", Eval.ParseEvalExpression("$sin(0)"));
        Assert.Equal("1", Eval.ParseEvalExpression("$cos(0)"));
        Assert.Equal("0", Eval.ParseEvalExpression("$tan(0)"));
        Assert.Equal(Math.Acos(0.5).ToString(), Eval.ParseEvalExpression("$asin(0.5)"));
        Assert.Equal(Math.Asin(0.5).ToString(), Eval.ParseEvalExpression("$acos(0.5)"));
        Assert.Equal(Math.Atan(1).ToString(), Eval.ParseEvalExpression("$atan(1)"));
        Assert.Equal("4", Eval.ParseEvalExpression("$eval(1+$eval(1+2))"));
    }

    [Fact]
    public void ParseEvalExpression_PreservesLegacyNumericFormats()
    {
        using var culture = new CultureScope("en-US");

        for (var digits = 0; digits <= 5; digits++)
        {
            var number = 1234.56789;

            Assert.Equal(
                Math.Round(number, digits).ToString(
                    digits == 0 ? "0" : $"0.{"0".PadLeft(digits, '0')}",
                    CultureInfo.CurrentCulture),
                Eval.ParseEvalExpression($"$round{digits}({number.ToString(CultureInfo.InvariantCulture)})"));
            Assert.Equal(
                number.ToString($"N{digits}", CultureInfo.CurrentCulture),
                Eval.ParseEvalExpression($"$n{digits}({number.ToString(CultureInfo.InvariantCulture)})"));
            Assert.Equal(
                number.ToString($"N{digits}", CultureInfo.GetCultureInfo("de-DE")),
                Eval.ParseEvalExpression($"$n{digits}_de({number.ToString(CultureInfo.InvariantCulture)})"));
        }
    }

    [Theory]
    [InlineData("$eval(", "Syntax error: $eval(")]
    [InlineData("$round2(", "Syntax error: $round2(")]
    [InlineData("$n2(", "Syntax error: $n2(")]
    [InlineData("$n2_de(", "Syntax error: $n2_de(")]
    [InlineData("$round2(not-a-number)", "NaN")]
    public void ParseEvalExpression_PreservesLegacyErrors(string expression, string expected)
    {
        using var culture = new CultureScope("en-US");

        Assert.Equal(expected, Eval.ParseEvalExpression(expression));
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string cultureName)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }
}
