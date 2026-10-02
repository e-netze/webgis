using E.Standard.Parsing.SimpleExpressions;
using System.Globalization;

namespace E.Standard.Parsing.Tests;

public class SimpleExpressionTests
{
    private sealed class TestResolver : ISimpleExpressionValueResolver
    {
        public Dictionary<string, string?> Values { get; } = new();
        public Dictionary<string, string?> SpecialKeys { get; } = new();

        public string? GetValue(string fieldName)
            => Values.TryGetValue(fieldName, out var value) ? value : String.Empty;

        public bool TryResolveSpecialKey(string key, out string? replacement)
            => SpecialKeys.TryGetValue(key, out replacement);

        public bool TryParseNumber(string? value, out double number)
            => Double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number);

        public string UrlEncodeLatin1(string? value) => $"latin1({value})";
    }

    private static string Solve(string expression, TestResolver resolver)
        => SimpleExpression.Solve(expression, KeyParameters.Parse(expression), resolver);

    [Fact]
    public void Solve_ReplacesFieldKeys()
    {
        var resolver = new TestResolver();
        resolver.Values["A"] = "1";
        resolver.Values["B"] = "2";

        Assert.Equal("1-2-1", Solve("[A]-[B]-[A]", resolver));
    }

    [Fact]
    public void Solve_ReturnsExpression_WhenNoKeysOrResolver()
    {
        Assert.Equal("[A]", SimpleExpression.Solve("[A]", null, new TestResolver()));
        Assert.Equal("[A]", SimpleExpression.Solve("[A]", ["A"], (TestResolver?)null));
    }

    [Fact]
    public void Solve_RequiredKey_ReturnsEmptyString_WhenValueIsEmpty()
    {
        var resolver = new TestResolver();
        resolver.Values["A"] = "x";

        Assert.Equal(String.Empty, Solve("[A] [!B]", resolver));

        resolver.Values["B"] = "y";
        Assert.Equal("x y", Solve("[A] [!B]", resolver));
    }

    [Fact]
    public void Solve_TildeKey_ReturnsFieldValue()
    {
        var resolver = new TestResolver();
        resolver.Values["A"] = "v";

        Assert.Equal("v", Solve("[~A]", resolver));
    }

    [Fact]
    public void Solve_FormatKey_FormatsNumbersAndKeepsStrings()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var resolver = new TestResolver();
            resolver.Values["N"] = "3.14159";
            resolver.Values["S"] = "abc";

            Assert.Equal("3.14 abc", Solve("[N:0.00] [S:0.00]", resolver));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Solve_UrlEncodeKeys()
    {
        var resolver = new TestResolver();
        resolver.Values["A"] = "a b&c";

        Assert.Equal("a%20b%26c latin1(a b&c)", Solve("[url-encode:A] [url-encode-latin1:A]", resolver));
    }

    [Fact]
    public void Solve_SpecialKeys_ReplaceOrKeepPlaceholder()
    {
        var resolver = new TestResolver();
        resolver.SpecialKeys["BBOX"] = "1,2,3,4";
        resolver.SpecialKeys["spatial::point"] = null;

        Assert.Equal("1,2,3,4 [spatial::point]", Solve("[BBOX] [spatial::point]", resolver));
    }

    [Fact]
    public void Solve_ReplacesSequentially_IncludingValuesContainingPlaceholders()
    {
        var resolver = new TestResolver();
        resolver.Values["A"] = "[B]";
        resolver.Values["B"] = "b";

        Assert.Equal("b b", Solve("[A] [B]", resolver));
    }

    [Fact]
    public void KeyParameters_SplitsSemicolonKeys_AndSkipsLeadingEmptyKeys()
    {
        Assert.Equal(["A", "B", "C"], KeyParameters.Parse("[;A;B] [C]")!);
        Assert.Null(KeyParameters.Parse("no keys"));
        Assert.Null(KeyParameters.Parse(null));
    }
}
