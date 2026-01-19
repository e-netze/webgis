using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.WebApp.Extensions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Configuration;
using Portal.Core.AppCode.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Middleware.Authentication;

public class OidcPrefixAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OidcPrefixAuthenticationMiddleware> _logger;

    public OidcPrefixAuthenticationMiddleware(
            RequestDelegate next,
            ILogger<OidcPrefixAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context,
                             IOptions<ApplicationSecurityConfig> appSecurityConfigOptions,
                             IEnumerable<IPortalAuthenticationService> _authenticationServices,
                             ConfigurationService config,
                             ITracerService tracer = null)
    {
        ApplicationSecurityConfig appSecurityConfig = appSecurityConfigOptions.Value;

        if (appSecurityConfig.UseOpenIdConnect() || appSecurityConfig.UseAzureAD())
        {
            if (context.User.Identity.IsAuthenticated)
            {
                _logger.LogClaims(LogLevel.Debug, context.User);
                //var authResult = await context.AuthenticateAsync("Cookies");
                //if (authResult.Succeeded)
                //{
                //    var idToken = authResult.Properties.GetTokenValue("id_token");
                //    _logger.Log(LogLevel.Debug, "Id-Token: {id_token}", idToken);
                //}

                var portalUser = context.User.ToPortalUser(appSecurityConfig);

                //Console.WriteLine("Username: " + portalUser.Username);
                //Console.WriteLine($" Roles [{ String.Join(", ", portalUser.UserRoles ?? new string[0]) }]");

                if (!portalUser.IsAnonymous && !portalUser.HasUsernamePrefix())
                {
                    if (appSecurityConfig.UseExtendedRolesFrom() == "windows")
                    {
                        var windowsUserName = context.User.Identity.Name.ToLowerInvariant();

                        if (windowsUserName.Contains("@"))
                        {
                            windowsUserName = $"{windowsUserName.Split('@')[1].Split(".")[0]}\\{windowsUserName.Split('@')[0]}";   // username@domain.com  => domain\username
                        }
                        else if (!windowsUserName.Contains("\\") && !String.IsNullOrEmpty(config[PortalConfigKeys.SecurityWindowsDomainSubstitute]))
                        {
                            windowsUserName = $"{config[PortalConfigKeys.SecurityWindowsDomainSubstitute].Split('=')[0].Trim()}\\{windowsUserName}";
                        }

                        var windowsUser = await _authenticationServices.GetService(UserType.WindowsUser)?
                                                                       .TryAuthenticationServiceUser(context, windowsUserName, true);

                        portalUser = new PortalUser(
                                windowsUser.Username,
                                windowsUser.UserRoles,
                                windowsUser.RoleParameters,
                                windowsUser.DisplayName
                            );

                        if (!portalUser.HasUsernamePrefix("nt-user"))
                        {
                            portalUser = portalUser.SetPrefixes("nt-user", "nt-group");
                        }
                    }
                    else
                    {
                        portalUser = portalUser.SetPrefixes("oidc-user", "oidc-role");
                    }
                }

                context.User = portalUser.ToClaimsPricipal();
            }
        }

        await _next(context);
    }
}
