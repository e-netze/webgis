using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using System.Security.Claims;

namespace WebGIS.API.MCP.Extensions.DependencyInjection;

internal static class ServiceCollectionExtensions
{
    static public IServiceCollection AddMcpAuthentication(this IServiceCollection services, IConfiguration config)
    {
        if (config.UseAuthentication())
        {
            services.AddAuthentication(options =>
            {
                options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Configure to validate tokens from our in-memory OAuth server
                options.Authority = config.GetAuthenticationAuthority();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = !String.IsNullOrEmpty(config.GetAuthenticationAudience()),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience = config.GetAuthenticationAudience(), // Validate that the audience matches the resource metadata as suggested in RFC 8707
                    ValidIssuer = config.GetAuthenticationAuthority(),
                    NameClaimType = "name",
                    RoleClaimType = "roles"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var name = context.Principal?.Identity?.Name ?? "unknown";
                        var email = context.Principal?.FindFirstValue("preferred_username") ?? "unknown";
                        Console.WriteLine($"Token validated for: {name} ({email})");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"Challenging client to authenticate with Entra ID");
                        return Task.CompletedTask;
                    }
                };
            })
            .AddMcp(options =>
            {
                options.ResourceMetadata = new()
                {
                    Resource = new Uri(config.GetMcpServerUrl()),
                    ResourceDocumentation = null,
                    AuthorizationServers = { new Uri(config.GetAuthenticationAuthority()) },
                    ScopesSupported = [/*"mcp:tools"*/"openid", "profile", "role", "email"],
                };
            });

            services.AddAuthorization();
        }
        return services;
    }
}
