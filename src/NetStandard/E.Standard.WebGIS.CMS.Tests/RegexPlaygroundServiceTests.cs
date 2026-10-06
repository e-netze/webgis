using E.Standard.WebGIS.CMS.Expressions;

namespace E.Standard.WebGIS.CMS.Tests;

public class RegexPlaygroundServiceTests
{
    [Theory]
    [InlineData("username", "ada_42", "42ada")]
    [InlineData("email", "ada@example.org", "ada@")]
    [InlineData("number", "-3,14", "3abc")]
    [InlineData("kg", "01001", "1001")]
    [InlineData("kg_styria", "60101", "01001")]
    [InlineData("parcel", ".123/4", "123/")]
    [InlineData("postcode", "8010", "0100")]
    public void Validate_ExamplesAcceptAndRejectInputs(string id, string valid, string invalid)
    {
        var example = RegexPlaygroundService.Examples.Single(item => item.Id == id);
        var result = RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            ValidationMode = true,
            Pattern = example.Pattern,
            Input = valid + "\n" + invalid + "\n"
        });
        Assert.True(result.Validations[0].Valid);
        Assert.False(result.Validations[1].Valid);
        Assert.False(result.Validations[2].Valid);
        Assert.False(result.ReplacementPreviewEnabled);
    }

    [Fact]
    public void Validate_PreservesWhitespaceAndChecksInputsSeparately()
    {
        var result = RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            ValidationMode = true,
            Pattern = "^[0-9]+$",
            Input = "123\r\n 123\n"
        });
        Assert.Equal(3, result.Validations.Count);
        Assert.True(result.Validations[0].Valid);
        Assert.False(result.Validations[1].Valid);
        Assert.Equal("", result.Validations[2].Input);
    }

    [Fact]
    public void Validate_RejectsMoreThanHundredInputs()
    {
        Assert.Throws<ArgumentException>(() => RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            ValidationMode = true,
            Pattern = ".",
            Input = new string('\n', 100)
        }));
    }

    [Fact]
    public void Evaluate_ReturnsMatchesAndNamedGroups()
    {
        var result = RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            Pattern = @"(?<key>\w+)=(?<value>\d+)",
            Input = "ID=42; SIZE=7"
        });

        Assert.Equal(2, result.MatchCount);
        Assert.False(result.MatchesTruncated);
        Assert.Equal("ID=42", result.Matches[0].Value);
        Assert.Contains(result.Matches[0].Groups, group => group.Name == "key" && group.Value == "ID");
        Assert.Contains(result.Matches[0].Groups, group => group.Name == "value" && group.Value == "42");
    }

    [Fact]
    public void Evaluate_AppliesRegexOptionsAndCreatesReplacementPreview()
    {
        var result = RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            Pattern = @"(?<name>ada)",
            Input = "ADA lovelace",
            Replacement = "Grace",
            IgnoreCase = true,
            PreviewReplacement = true
        });

        Assert.Equal(1, result.MatchCount);
        Assert.Equal("Grace lovelace", result.ReplacementPreview);
        Assert.False(result.ReplacementTruncated);
    }

    [Fact]
    public void Evaluate_ReportsOnlyFirstHundredMatches()
    {
        var result = RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            Pattern = "a",
            Input = new string('a', 101)
        });

        Assert.Equal(100, result.MatchCount);
        Assert.True(result.MatchesTruncated);
    }

    [Fact]
    public void Evaluate_SupportsMultilineAndSinglelineOptions()
    {
        var multiline = RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            Pattern = "^b$",
            Input = "a\nb\nc",
            Multiline = true
        });
        var singleline = RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            Pattern = "a.b",
            Input = "a\nb",
            Singleline = true
        });

        Assert.Equal("b", Assert.Single(multiline.Matches).Value);
        Assert.Equal("a\nb", Assert.Single(singleline.Matches).Value);
    }

    [Fact]
    public void Evaluate_LimitsReplacementPreviewLength()
    {
        var result = RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
        {
            Pattern = ".",
            Input = new string('a', RegexPlaygroundService.MaxInputLength),
            Replacement = new string('x', RegexPlaygroundService.MaxReplacementLength),
            PreviewReplacement = true
        });

        Assert.True(result.ReplacementTruncated);
        Assert.Equal(100_000, result.ReplacementPreview.Length);
    }

    [Fact]
    public void Evaluate_RejectsInputsAboveConfiguredLimits()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RegexPlaygroundService.Evaluate(new RegexPlaygroundRequest
            {
                Pattern = ".",
                Input = new string('a', RegexPlaygroundService.MaxInputLength + 1)
            }));

        Assert.Contains("test text", exception.Message);
    }
}
