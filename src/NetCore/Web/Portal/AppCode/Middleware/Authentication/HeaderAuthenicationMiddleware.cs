using E.Standard.Security.App.Json;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Services.Authentication;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Middleware.Authentication;

public class HeaderAuthenicationMiddleware
{
    private readonly RequestDelegate _next;

    public HeaderAuthenicationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext,
                             HeaderAuthenticationService headerAuthenticationService,
                             IOptions<ApplicationSecurityConfig> appSecurityConfigOptions,
                             ITracerService tracer)
    {
        if (httpContext.User.ApplyAuthenticationMiddleware())
        {
            var user = headerAuthenticationService.GetUser(httpContext);

            if (user != null)
            {
                httpContext.User = new PortalUser(
                    username: user.Username,
                    userRoles: user.Roles,
                    roleParameters: user.RoleParameters).ToClaimsPricipal();

                tracer.TracePortalUser(this, httpContext, appSecurityConfigOptions.Value);
            }
        }

        await _next.Invoke(httpContext);
    }
}
