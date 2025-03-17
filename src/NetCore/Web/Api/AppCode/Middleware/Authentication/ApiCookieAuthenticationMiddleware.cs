using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Authentication;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.OpenIdConnect.Extensions;
using E.Standard.Security.App.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware.Authentication;

public class ApiCookieAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiConfigurationService _apiConfig;

    public ApiCookieAuthenticationMiddleware(RequestDelegate next,
                                             ApiConfigurationService config)
    {
        _next = next;
        _apiConfig = config;
    }

    public async Task Invoke(HttpContext httpContext,
                             RoutingEndPointReflectionService endpointReflection,
                             ApiCookieAuthenticationService cookies,
                             IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfigMonitor)
    {
        if (httpContext.User.ApplyAuthenticationMiddleware(endpointReflection, ApiAuthenticationTypes.Cookie))
        {
            var appSecurityConfig = appSecurityConfigMonitor.CurrentValue;

            CmsDocument.UserIdentification ui = null;

            if (appSecurityConfig?.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection ||
                appSecurityConfig?.IdentityType == ApplicationSecurityIdentityTypes.AzureAD)
            {
                string username = httpContext.User.GetUsername();
                string userId = httpContext.User.GetUserId();
                string displayName = String.Empty;

                #region Map user to an fixed username => different users can use same username (admins)

                var mappedUser = appSecurityConfig
                        .MapUsers?
                        .Where(u => u.MapUsernames != null && u.MapUsernames.Contains(username))
                        .FirstOrDefault();

                if (mappedUser != null)
                {
                    userId = mappedUser.UserId;
                    displayName = $"{username} (alias {mappedUser.Name})";

                    username = mappedUser.Name;
                }

                #endregion

                ui = new CmsDocument.UserIdentification(
                    username,
                    httpContext.User.GetRoles()?.ToArray(),
                    null,
                    _apiConfig.InstanceRoles,
                    userId: userId,
                    displayName: displayName);
            }
            else
            {
                string[] usernameParts = cookies.TryGetCookieUsername(httpContext)?.Split(':');

                // Api Subscriber Cookies:
                //              subscriber:UserId:Userame
                //              ower:UserId:Username
                if (usernameParts?.Length == 3)
                {
                    ui = new CmsDocument.UserIdentification(
                                usernameParts[2],
                                new[] { usernameParts[0] },
                                null,
                                _apiConfig.InstanceRoles,
                                userId: usernameParts[1]);
                }
            }

            if (ui != null)
            {
                httpContext.User = ui.ToClaimsPrincipal(ApiAuthenticationTypes.Cookie);
            }
        }

        await _next(httpContext);
    }
}
