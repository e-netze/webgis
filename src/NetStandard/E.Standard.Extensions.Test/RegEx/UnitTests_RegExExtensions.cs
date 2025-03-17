using E.Standard.Extensions.RegEx;
using Xunit;

namespace E.Standard.Extensions.Test.RegEx;

public class UnitTests_RegExExtensions
{
    [Theory]
    [InlineData("%a%b%c%", "^.*a.*b.*c.*$")]
    [InlineData("abc", "^abc$")]
    [InlineData("a*b*c*", "^a.*b.*c.*$")]
    public void WildCardToRegular_ShouldConvertWildcardToRegularExpression(string value, string expected)
    {
        // Act
        string result = value.WildCardToRegular();

        // Assert
        Assert.Equal(expected, result);
    }
}
