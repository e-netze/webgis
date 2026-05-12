using E.Standard.Extensions.Compare;
using E.Standard.Security.Cryptography.Services;
using E.Standard.WebApp.Abstraction;
using E.Standard.WebApp.Extensions;
using E.Standard.WebApp.Options;
using E.Standard.WebApp.Reflection;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace E.Standard.WebApp.Middleware;

internal class EndpointAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public EndpointAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    async public Task Invoke(HttpContext httpContext, 
                             IEndPointReflectionProvider endPointReflectionProvider,
                             JwtAccessTokenService tokenService,
                             IOptions<SecurityOptions> options)
    {
        var endpointAuth = endPointReflectionProvider.GetCustomAttribute<EndpointAuthorizationAttribute>();
        if (endpointAuth is null)
        {
            await _next(httpContext);
            return;
        }

        var securityOptions = options.Value;

        bool? isAuthorized = 
            PerformLocalhostAuthorization(endpointAuth, httpContext)
            .OrTake(PerformUrlParameterAuthorization(endpointAuth, httpContext, securityOptions))
            .OrTake(PerformBasicAuthorization(endpointAuth, httpContext, securityOptions))
            .OrTake(PerformBearerToken(endpointAuth, tokenService, httpContext, securityOptions));

        if (isAuthorized.HasValue && isAuthorized.Value == false)
        {
            if (securityOptions.HasBasicAuthentication
                && endpointAuth.AuthorizationType.HasFlag(EndpointAuthorizationType.Basic))
            {
                httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Restricted Area\", charset=\"UTF-8\"";
            }

            httpContext.Response.StatusCode = 401;
            await httpContext.Response.WriteAsync("401 Unauthorized");

            return;
        }

        await _next(httpContext);
    }

    private bool? PerformLocalhostAuthorization(
            EndpointAuthorizationAttribute endpointAuth,
            HttpContext httpContext)
    {
        if (!endpointAuth.AuthorizationType.HasFlag(EndpointAuthorizationType.Localhost))
        {
            return null;
        }

        var connection = httpContext.Connection;

        // Ensure that the X-Forwarded-For header is not set
        // (this would indicate a proxy/ external client)
        if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            return false;
        }

        var isLocalhost = connection.RemoteIpAddress is not null
            && (connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                || System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress));  // eg. 127.0.0.1

        return isLocalhost;
    }

    private bool? PerformBearerToken(
            EndpointAuthorizationAttribute endpointAuth,
            JwtAccessTokenService tokenService,
            HttpContext httpContext,
            SecurityOptions securityOptions)
    {
        if (!endpointAuth.AuthorizationType.HasFlag(EndpointAuthorizationType.BearerToken))
        {
            return null;
        }

        var token = ExtractBearerToken(httpContext);
        if (String.IsNullOrEmpty(token))
        {
            if(endpointAuth.AllowIfNotConfigured)
            {
                return null;
            }

            return false;
        }

        try
        {
            var user = tokenService.ValidateToken(token);
            return securityOptions.EndpointAuthorizationBearerUsername.Equals(user?.Identity?.Name);
        }
        catch
        {
            return false;
        }
    }

    private string? ExtractBearerToken(HttpContext httpContext)
    {
        var authHeader = httpContext.Request.Headers["Authorization"].ToString();
        if (authHeader.StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        var queryToken = httpContext.Request.Query["token"].ToString();
        return String.IsNullOrEmpty(queryToken) ? null : queryToken;
    }

    private bool? PerformBasicAuthorization(
            EndpointAuthorizationAttribute endpointAuth, 
            HttpContext httpContext, 
            SecurityOptions securityOptions)
    {
        if (!endpointAuth.AuthorizationType.HasFlag(EndpointAuthorizationType.Basic))
        {
            return null;
        }

        if (!securityOptions.HasBasicAuthentication
            && endpointAuth.AllowIfNotConfigured)
        {
            return null;
        }

        var authHeader = httpContext.Request.Headers["Authorization"].ToString();

        if (!authHeader.StartsWith("Basic ", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        var authCode = System.Text.Encoding.ASCII.GetString(
            Convert.FromBase64String(authHeader.Substring("Basic ".Length)));

        var pos = authCode.IndexOf(":");
        if (pos < 0)
        {
            return false;
        }

        var username = authCode.Substring(0, pos);
        var password = authCode.Substring(pos + 1);

        return securityOptions.EndpointAuthorizationBasicUsername.Equals(username)
            && securityOptions.EndpointAuthorizationBasicPassword.Equals(password);
    }

    private bool? PerformUrlParameterAuthorization(
            EndpointAuthorizationAttribute endpointAuth, 
            HttpContext httpContext, 
            SecurityOptions securityOptions)
    {
        if (!endpointAuth.AuthorizationType.HasFlag(EndpointAuthorizationType.UrlPassword))
        {
            return null;
        }

        if (!securityOptions.HasUrlPasswordAuthentication 
            && endpointAuth.AllowIfNotConfigured)
        {
            return null;
        }

        var urlPassword = httpContext.Request.Query["pwd"].ToString()
                            .OrTake(httpContext.Request.Query["password"].ToString());

        return securityOptions.EndpointAuthorizationUrlPassword.Equals(urlPassword);
    }
}
