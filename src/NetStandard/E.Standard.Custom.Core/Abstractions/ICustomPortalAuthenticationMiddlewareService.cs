using System.Threading.Tasks;

using E.Standard.Custom.Core.Models;

using Microsoft.AspNetCore.Http;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomPortalAuthenticationMiddlewareService
{
    bool ForceInvoke(HttpContext httpContext);

    Task<CustomAuthenticationUser> InvokeFromMiddleware(HttpContext httpContext);
}
