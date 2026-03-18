using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using E.Standard.WebGIS.Core.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services.Authentication;

#if DEBUG

public class DebugCustomAuthenticationMiddlewareService : ICustomPortalAuthenticationMiddlewareService
{
    private readonly ITracerService _tracer;

    public DebugCustomAuthenticationMiddlewareService(ITracerService tracer)
        => (_tracer) = (tracer);

    public bool ForceInvoke(HttpContext httpContext) => true;

    public Task<CustomAuthenticationUser> InvokeFromMiddleware(HttpContext httpContext)
    {
        if (httpContext.User?.Identity != null && httpContext.User.Identity.IsAuthenticated)
        {
            //return Task.FromResult(new CustomAuthenticationUser()
            //{
            //    Roles = new[] { "debug-role1", "debug-role2" },
            //    RoleParameters = new[] { "debug-roleparameter1=1", "debug-roleparameter2=2" },
            //    AppendRolesAndParameters = true
            //});
        }

        return Task.FromResult<CustomAuthenticationUser>(null);
    }
}

#endif