using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace E.Standard.Web.Models;

public class RequestAuthorization
{
    public RequestAuthorization(string authType = "Basic", string urlTokenParameterName = "token")
    {
        this.AuthType = authType;
        this.UrlTokenParameterName = urlTokenParameterName;
    }

    public string AuthType { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string AccessToken { get; set; } = "";

    public string UrlTokenParameterName { get; set; } = "";
    public string UrlToken { get; set; } = "";

    public X509Certificate? ClientCerticate { get; set; }

    public ICredentials? Credentials { get; set; }

    public bool UseDefaultCredentials { get; set; }

    public static RequestAuthorization BasicAuthentication(string username, string password)
        => new RequestAuthorization(authType: "Basic")
        {
            Username = username,
            Password = password
        };

    public static RequestAuthorization BearerAuthentication(string accessToken)
        => new RequestAuthorization(authType: "Bearer")
        {
            AccessToken = accessToken
        };

    public static RequestAuthorization? FromHttpAuthSchemeOrNull(string username = "", string password = "", string accessToken = "")
        => (!string.IsNullOrEmpty(username), !string.IsNullOrEmpty(password), !string.IsNullOrEmpty(accessToken)) switch
        {
            (true, true, _) => RequestAuthorization.BasicAuthentication(username, password),
            (_, _, true) => RequestAuthorization.BearerAuthentication(accessToken),
            (_, _, _) => null
        };
}
