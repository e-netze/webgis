using E.Standard.Custom.Core.Abstractions;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
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
                             IEnumerable<ICustomPortalAuthenticationMiddlewareService> customAuthentications = null)
    {
        if (customAuthentications != null)
        {
            foreach (var customAuthentication in customAuthentications)
            {
                if (customAuthentication.ForceInvoke(httpContext) == true || httpContext.User.ApplyAuthenticationMiddleware())
                {
                    var customAuthUser = await customAuthentication.InvokeFromMiddleware(httpContext);

                    if (customAuthUser != null && customAuthUser.AppendRolesAndParameters == true)
                    {
                        var portalUser = httpContext.User.ToPortalUser();

                        portalUser.AddRoleParameters(customAuthUser.RoleParameters);
                        portalUser.AddRoles(customAuthUser.Roles);

                        httpContext.User = portalUser.ToClaimsPricipal();

                        tracer.Log(this, $"AppendRoles and Parameters user from {customAuthentication.GetType()}");
                        tracer.TracePortalUser(this, httpContext);
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
                        tracer.TracePortalUser(this, httpContext);
                    }
                }
            }
        }

        await _next(httpContext);
    }
}
