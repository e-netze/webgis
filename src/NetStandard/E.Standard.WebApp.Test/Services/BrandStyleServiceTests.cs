using System;
using System.Reflection;
using System.Text.RegularExpressions;

using E.Standard.WebApp.Services;

namespace E.Standard.WebApp.Test.Services;

public class BrandStyleServiceTests : IDisposable
{
    private const string PrimaryVar = "CSS_WEBGIS_BRAND_PRIMARY";
    private const string PrimaryLightVar = "CSS_WEBGIS_BRAND_PRIMARY_LIGHT";
    private const string LogoVar = "CSS_WEBGIS_BRAND_LOGO";

    public BrandStyleServiceTests()
    {
        // Ensure a clean slate before every test.
        Environment.SetEnvironmentVariable(PrimaryVar, null);
        Environment.SetEnvironmentVariable(PrimaryLightVar, null);
        Environment.SetEnvironmentVariable(LogoVar, null);
    }

    public void Dispose()
    {
        // Ensure a clean slate after every test as well.
        Environment.SetEnvironmentVariable(PrimaryVar, null);
        Environment.SetEnvironmentVariable(PrimaryLightVar, null);
        Environment.SetEnvironmentVariable(LogoVar, null);
    }

    [Fact]
    public void GetBrandVariablesFromEnvironment_NoVariablesSet_ReturnsEmptyString()
    {
        var service = new BrandStyleService();

        var result = service.GetBrandVariablesFromEnvironment();

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData("#a00")]
    [InlineData("#AA0000")]
    [InlineData("#aa0000ff")]
    [InlineData("rgb(170,0,0)")]
    [InlineData("rgba(170,0,0,0.5)")]
    [InlineData("hsl(0,100%,33%)")]
    [InlineData("hsla(0,100%,33%,0.5)")]
    [InlineData("oklch(0.5 0.2 30)")]
    [InlineData("color-mix(in srgb, red 50%, white)")]
    [InlineData("var(--some-other-var)")]
    [InlineData("red")]
    public void GetBrandVariablesFromEnvironment_ValidColorValue_IsRendered(string value)
    {
        Environment.SetEnvironmentVariable(PrimaryVar, value);
        var service = new BrandStyleService();

        var result = service.GetBrandVariablesFromEnvironment();

        Assert.Equal($"--webgis-brand-primary:{value};", result);
    }

    [Theory]
    [InlineData("red; } </style><script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("\"><img src=x onerror=alert(1)>")]
    [InlineData("red}body{background:url(javascript:alert(1))")]
    [InlineData("   ")]
    public void GetBrandVariablesFromEnvironment_InvalidOrMaliciousValue_IsDiscarded(string value)
    {
        Environment.SetEnvironmentVariable(PrimaryVar, value);
        var service = new BrandStyleService();

        var result = service.GetBrandVariablesFromEnvironment();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetBrandVariablesFromEnvironment_BothVariablesSet_RendersBothInOrder()
    {
        Environment.SetEnvironmentVariable(PrimaryVar, "#a00");
        Environment.SetEnvironmentVariable(PrimaryLightVar, "#faa");
        var service = new BrandStyleService();

        var result = service.GetBrandVariablesFromEnvironment();

        Assert.Equal("--webgis-brand-primary:#a00;--webgis-brand-primary-light:#faa;", result);
    }

    [Fact]
    public void GetBrandVariablesFromEnvironment_OnlyValidVariableIsRendered_WhenOtherIsInvalid()
    {
        Environment.SetEnvironmentVariable(PrimaryVar, "#a00");
        Environment.SetEnvironmentVariable(PrimaryLightVar, "red; } </style><script>alert(1)</script>");
        var service = new BrandStyleService();

        var result = service.GetBrandVariablesFromEnvironment();

        Assert.Equal("--webgis-brand-primary:#a00;", result);
    }

    [Fact]
    public void GetBrandVariablesFromEnvironment_ResultIsCached_AfterFirstAccess()
    {
        Environment.SetEnvironmentVariable(PrimaryVar, "#a00");
        var service = new BrandStyleService();

        var first = service.GetBrandVariablesFromEnvironment();

        // Changing the environment variable after the first access must not affect
        // the cached result, since the value is computed lazily only once.
        Environment.SetEnvironmentVariable(PrimaryVar, "#b00");
        var second = service.GetBrandVariablesFromEnvironment();

        Assert.Equal(first, second);
        Assert.Equal("--webgis-brand-primary:#a00;", second);
    }

    // The service currently exposes no Length-typed brand variable, so the
    // Length validation logic is tested directly via the private regex to
    // ensure it supports CSS shorthand notation with 1 to 4 space-separated
    // tokens, e.g. "4px", "4px 10px" or "4px 10px 4px 10px".
    private static readonly Regex _validCssLengthValueRegex =
        (Regex)typeof(BrandStyleService)
            .GetField("_validCssLengthValue", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    [Theory]
    [InlineData("4px")]
    [InlineData("0")]
    [InlineData("1.5rem")]
    [InlineData("-4px")]
    [InlineData("var(--some-length)")]
    [InlineData("calc(1rem + 2px)")]
    [InlineData("4px 10px")]
    [InlineData("4px 10px 4px 10px")]
    [InlineData("4px 10px 4px")]
    [InlineData("0 0 0 0")]
    [InlineData("1rem var(--some-length)")]
    [InlineData("calc(1rem + 2px) 10px")]
    public void ValidCssLengthValue_ValidLengthValue_IsMatched(string value)
    {
        Assert.Matches(_validCssLengthValueRegex, value);
    }

    [Theory]
    [InlineData("4px 10px 4px 10px 4px")]   // more than 4 tokens
    [InlineData("4px;color:red")]
    [InlineData("javascript:alert(1)")]
    [InlineData("4px 10px;}</style>")]
    [InlineData("")]
    [InlineData("4px  ")]
    [InlineData("4pxx")]
    public void ValidCssLengthValue_InvalidLengthValue_IsNotMatched(string value)
    {
        Assert.DoesNotMatch(_validCssLengthValueRegex, value);
    }

    private static readonly Regex _validCssUrlValueRegex =
        (Regex)typeof(BrandStyleService)
            .GetField("_validCssUrlValue", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    [Theory]
    [InlineData("url(https://example.com/logo.png)")]
    [InlineData("url(http://example.com/logo.png)")]
    [InlineData("url('https://example.com/logo.png')")]
    [InlineData("url(\"https://example.com/logo.png\")")]
    [InlineData("url(//example.com/logo.png)")]
    [InlineData("url(/content/logo.png)")]
    [InlineData("url( https://example.com/logo.png )")]
    public void ValidCssUrlValue_ValidUrlValue_IsMatched(string value)
    {
        Assert.Matches(_validCssUrlValueRegex, value);
    }

    [Theory]
    [InlineData("url(javascript:alert(1))")]
    [InlineData("url(data:text/html,<script>alert(1)</script>)")]
    [InlineData("url(logo.png)")]                              // missing scheme/root-relative marker
    [InlineData("url(https://example.com/logo.png); } </style><script>alert(1)</script>")]
    [InlineData("url()")]
    [InlineData("")]
    public void ValidCssUrlValue_InvalidUrlValue_IsNotMatched(string value)
    {
        Assert.DoesNotMatch(_validCssUrlValueRegex, value);
    }

    [Fact]
    public void GetBrandVariablesFromEnvironment_ValidLogoUrlValue_IsRendered()
    {
        Environment.SetEnvironmentVariable(LogoVar, "url(https://example.com/logo.png)");
        var service = new BrandStyleService();

        var result = service.GetBrandVariablesFromEnvironment();

        Assert.Equal("--webgis-brand-logo:url(https://example.com/logo.png);", result);
    }

    [Fact]
    public void GetBrandVariablesFromEnvironment_InvalidLogoUrlValue_IsDiscarded()
    {
        Environment.SetEnvironmentVariable(LogoVar, "url(javascript:alert(1))");
        var service = new BrandStyleService();

        var result = service.GetBrandVariablesFromEnvironment();

        Assert.Equal(string.Empty, result);
    }
}
