using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Portal.Core.AppCode.Extensions;

namespace Portal.Core.AppCode.Services;

public class WebgisPortalCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebgisPortalCorsPolicyProvider> _logger;

    public WebgisPortalCorsPolicyProvider(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<WebgisPortalCorsPolicyProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<CorsPolicy> GetPolicyAsync(
        HttpContext context,
        string policyName)
    {
        var builder = new CorsPolicyBuilder();

        if (policyName == "hmac")
        {
            builder.WithOrigins(AllowedHmacUrls(context));
            builder.AllowCredentials();
        }
        else
        {
            var allowedOrigins = AllowedDefaultUrls(context);
            string origin = context.Request.Headers["Origin"].ToString();

            if (!String.IsNullOrEmpty(origin)
                && !allowedOrigins.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase)
                && _configuration.CorsDefaultAllowAnyOriginWithoutCredentials())
            {
                // Untrusted origin: allowed only if explicitly configured, never with credentials
                builder.AllowAnyOrigin();
            }
            else
            {
                builder.WithOrigins(allowedOrigins);
                builder.AllowCredentials();
            }
        }

        builder
            .AllowAnyHeader()
            .AllowAnyMethod();

        return Task.FromResult(builder.Build());
    }

    private string[] AllowedDefaultUrls(HttpContext context)
    {
        using var scope = _scopeFactory.CreateScope();

        var urlLHelper = scope.ServiceProvider
            .GetRequiredService<UrlHelperService>();

        string[] configCorsUrls = _configuration.AdditionalCorsOriginsFor("default")
            .Select(u => u switch
            {
                "~" => context.Request.Headers["Origin"].ToString(),
                _ => u
            })
            .ToArray();

        return new[]
            {
                urlLHelper.AppRootUrlFromConfig(context.Request, false),
                urlLHelper.ApiUrl(context.Request)
            }
            .Concat(configCorsUrls)
            .Select(ToOrigin)
            .Where(u => !String.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // Reduces a url to scheme://host[:port], as sent in the Origin header
    private static string ToOrigin(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority)
            : null;

    private string[] AllowedHmacUrls(HttpContext context)
    {
        using var scope = _scopeFactory.CreateScope();

        var urlLHelper = scope.ServiceProvider
            .GetRequiredService<UrlHelperService>();

        string portalUrl = urlLHelper.AppRootUrlFromConfig(context.Request, false);
        string apiUrl = urlLHelper.ApiUrl(context.Request);

        string[] configCorsUrls = _configuration.AdditionalCorsOriginsFor("hmac")
            .Select(u => u switch
            {
                "~" => context.Request.Headers["Origin"].ToString(),
                _ => u
            })
            .Where(u => !String.IsNullOrWhiteSpace(u))
            .ToArray();

        return [portalUrl, apiUrl, .. configCorsUrls];
    }
}
