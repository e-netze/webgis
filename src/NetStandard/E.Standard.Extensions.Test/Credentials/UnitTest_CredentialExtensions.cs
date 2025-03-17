using E.Standard.Extensions.Credentials;
using Xunit;

namespace E.Standard.Extensions.Test.Credentials;

public class UnitTest_CredentialExtensions
{
    [Theory]
    [InlineData(@"DOMAIN\username", "username")]
    [InlineData("username::substring", "substring")]
    public void PureUsername_ShouldRemoveDomain(string username, string expected)
    {
        // Act
        string pureUsername = username.PureUsername();

        // Assert
        Assert.Equal(expected, pureUsername);
    }

    [Theory]
    [InlineData(@"DOMAIN\username", 4, "User")]
    [InlineData("username", 10, "Username")]
    public void ShortPureUsername_ShouldReturnShortenedUsername(string username, int length, string expected)
    {
        // Act
        string shortUsername = username.ShortPureUsername(length);

        // Assert
        Assert.Equal(expected, shortUsername);
    }

}
