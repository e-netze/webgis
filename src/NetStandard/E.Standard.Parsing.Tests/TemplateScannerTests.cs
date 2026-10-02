using E.Standard.Parsing.SimpleExpressions;

namespace E.Standard.Parsing.Tests;

public class TemplateScannerTests
{
    [Fact]
    public void Scan_PreservesOrderDuplicatesAndPositions()
    {
        var result = TemplateScanner.Scan("x[ONE]-[TWO]-[ONE]");

        Assert.Equal(["ONE", "TWO", "ONE"], result.Select(item => item.Key));
        Assert.Equal([1, 7, 13], result.Select(item => item.Start));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData("[unfinished")]
    public void Scan_ReturnsEmptyForTemplatesWithoutCompleteParameters(string template)
    {
        Assert.Empty(TemplateScanner.Scan(template));
    }

    [Fact]
    public void Replace_UsesEachOccurrenceAndCanPreserveUnknownParameters()
    {
        var result = TemplateScanner.Replace(
            "[A]-[B]-[A]",
            parameter => parameter.Key == "A" ? "x" : null);

        Assert.Equal("x-[B]-x", result);
    }
}
