using E.Standard.Extensions.Formatting;
using Xunit;

namespace E.Standard.Extensions.Test.Formatting;

public class UnitTests_FormattingExtensions
{
    [Theory]
    [InlineData("1,5", "1.5")]
    public void ToInvariantNumberString_ShouldReplaceCommaWithDotDouble(string number, string expected)
    {
        // Act
        string result = number.ToInvariantNumberString<double>();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1,5", "1.5")]
    public void ToInvariantNumberString_ShouldReplaceCommaWithDotFloat(string number, string expected)
    {
        // Act
        string result = number.ToInvariantNumberString<float>();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1,5", "1.5")]
    public void ToInvariantNumberString_ShouldReplaceCommaWithDotDecimal(string number, string expected)
    {
        // Act
        string result = number.ToInvariantNumberString<decimal>();

        // Assert
        Assert.Equal(expected, result);
    }
}
