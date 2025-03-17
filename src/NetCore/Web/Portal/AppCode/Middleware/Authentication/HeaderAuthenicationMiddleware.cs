using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
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

                tracer.TracePortalUser(this, httpContext);
            }
        }

        await _next.Invoke(httpContext);
    }
}
