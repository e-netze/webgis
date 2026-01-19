using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Json;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Middleware.Authentication;

public class CustomAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public CustomAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    async public Task Invoke(HttpContext httpContext,
                             WebgisCookieService cookies,
                             ITracerService tracer,
                             IOptions<ApplicationSecurityConfig> appSecurityConfigOptions,
                             IEnumerable<ICustomPortalAuthenticationMiddlewareService> customAuthentications = null)
    {
        if (customAuthentications != null)
        {
            foreach (var customAuthentication in customAuthentications)
            {
                if (customAuthentication.ForceInvoke(httpContext) == true || httpContext.User.ApplyAuthenticationMiddleware())
                {
                    var customAuthUser = await customAuthentication.InvokeFromMiddleware(httpContext);
                    var appSecurityConfig = appSecurityConfigOptions.Value;

                    if (customAuthUser != null && customAuthUser.AppendRolesAndParameters == true)
                    {
                        var portalUser = httpContext.User.ToPortalUser(appSecurityConfig);
                        portalUser.AddRoleParameters(customAuthUser.RoleParameters);
                        portalUser.AddRoles(customAuthUser.Roles);

                        httpContext.User = portalUser.ToClaimsPricipal();

                        tracer.Log(this, $"AppendRoles and Parameters user from {customAuthentication.GetType()}");
                        tracer.TracePortalUser(this, httpContext, appSecurityConfig);
                    }
                    else if (!String.IsNullOrEmpty(customAuthUser?.Username))
                    {
                        if (customAuthUser.SetCookie == true)
                        {
                            cookies.SetAuthCookie(httpContext, false, customAuthUser.Username, customAuthUser.UserType, expires: customAuthUser.Expires, userRoles: customAuthUser.Roles);
                        }

                        httpContext.User = new PortalUser(customAuthUser.Username, userRoles: customAuthUser.Roles,
                                                                                   roleParameters: customAuthUser.RoleParameters).ToClaimsPricipal();

                        tracer.Log(this, $"Authenticated user from {customAuthentication.GetType()}");
                        tracer.TracePortalUser(this, httpContext, appSecurityConfig);
                    }
                }
            }
        }

        await _next(httpContext);
    }
}
