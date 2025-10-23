using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portal.Core.AppCode.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services;

public class WebgisPortalCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public WebgisPortalCorsPolicyProvider(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public Task<CorsPolicy> GetPolicyAsync(
        HttpContext context,
        string policyName)
    {
        var allowedOrigins =
            policyName switch
            {
                "hmac" => AllowedHmacUrls(context),
                _ => [
                    String.IsNullOrEmpty(context.Request.Headers["Origin"])
                        ? "*"
                        : context.Request.Headers["Origin"]
                    ]
            };

        var builder = new CorsPolicyBuilder();

        if (!allowedOrigins.Contains("*"))
        {
            builder.WithOrigins(allowedOrigins);
            builder.AllowCredentials();
        }
        else
        {
            builder.AllowAnyOrigin();
        }

        builder
            .AllowAnyHeader()
            .AllowAnyMethod();

        return Task.FromResult(builder.Build());
    }

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
