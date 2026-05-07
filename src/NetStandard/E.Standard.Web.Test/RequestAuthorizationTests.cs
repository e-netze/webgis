using E.Standard.Web.Models;

namespace E.Standard.Web.Test;

public class RequestAuthorizationTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsAuthTypeAndUrlTokenParameterName()
    {
        var auth = new RequestAuthorization();

        Assert.Equal("Basic", auth.AuthType);
        Assert.Equal("token", auth.UrlTokenParameterName);
    }

    [Fact]
    public void Constructor_CustomValues_SetsAuthTypeAndUrlTokenParameterName()
    {
        var auth = new RequestAuthorization(authType: "Bearer", urlTokenParameterName: "access_token");

        Assert.Equal("Bearer", auth.AuthType);
        Assert.Equal("access_token", auth.UrlTokenParameterName);
    }

    [Fact]
    public void BasicAuthentication_SetsUsernamePasswordAndAuthType()
    {
        var auth = RequestAuthorization.BasicAuthentication("user", "pass");

        Assert.Equal("Basic", auth.AuthType);
        Assert.Equal("user", auth.Username);
        Assert.Equal("pass", auth.Password);
    }

    [Fact]
    public void BearerAuthentication_SetsAccessTokenAndAuthType()
    {
        var auth = RequestAuthorization.BearerAuthentication("mytoken");

        Assert.Equal("Bearer", auth.AuthType);
        Assert.Equal("mytoken", auth.AccessToken);
    }

    [Fact]
    public void AuthenticationOrNull_WithUsernameAndPassword_ReturnsBasicAuthentication()
    {
        var auth = RequestAuthorization.FromHttpAuthSchemeOrNull("user", "pass");

        Assert.NotNull(auth);
        Assert.Equal("Basic", auth.AuthType);
        Assert.Equal("user", auth.Username);
        Assert.Equal("pass", auth.Password);
    }

    [Fact]
    public void AuthenticationOrNull_WithAccessTokenOnly_ReturnsBearerAuthentication()
    {
        var auth = RequestAuthorization.FromHttpAuthSchemeOrNull(accessToken: "mytoken");

        Assert.NotNull(auth);
        Assert.Equal("Bearer", auth.AuthType);
        Assert.Equal("mytoken", auth.AccessToken);
    }

    [Fact]
    public void AuthenticationOrNull_WithAllEmpty_ReturnsNull()
    {
        var auth1 = RequestAuthorization.FromHttpAuthSchemeOrNull("", "", "");
        var auth2 = RequestAuthorization.FromHttpAuthSchemeOrNull();
        var auth3 = RequestAuthorization.FromHttpAuthSchemeOrNull("user");
        var auth4 = RequestAuthorization.FromHttpAuthSchemeOrNull("", "pass");

        Assert.Null(auth1);
        Assert.Null(auth2);
        Assert.Null(auth3);
        Assert.Null(auth4);
    }

    [Fact]
    public void AuthenticationOrNull_WithUsernamePasswordAndToken_PrefersBasicAuthentication()
    {
        var auth = RequestAuthorization.FromHttpAuthSchemeOrNull("user", "pass", "mytoken");

        Assert.NotNull(auth);
        Assert.Equal("Basic", auth.AuthType);
    }

    [Fact]
    public void DefaultPropertyValues_AreEmptyStrings()
    {
        var auth = new RequestAuthorization();

        Assert.Equal("", auth.Username);
        Assert.Equal("", auth.Password);
        Assert.Equal("", auth.AccessToken);
        Assert.Equal("", auth.UrlToken);
        Assert.Null(auth.ClientCerticate);
        Assert.Null(auth.Credentials);
        Assert.False(auth.UseDefaultCredentials);
    }
}
