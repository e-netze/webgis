using E.Standard.Extensions.IO;
using Xunit;

namespace E.Standard.Extensions.Test.IO;

public class UnitTests_IOExtensions
{
    [Theory]
    [InlineData("http://www.google.com", true)]
    [InlineData("https://www.google.com", true)]
    [InlineData("ftp://www.google.com", false)]
    [InlineData("www.google.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidHttpUrl_ShouldCheckForValidHttpUrl(string s, bool expected)
    {
        // Act
        bool result = s.IsValidHttpUrl();

        // Assert
        Assert.Equal(expected, result);
    }
}
