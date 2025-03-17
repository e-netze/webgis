using E.Standard.Custom.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomApiAuthenticationMiddlewareService
{
    ApiAuthenticationTypes AuthTypes { get; }

    Task<CustomAuthenticationUser> InvokeFromMiddleware(HttpContext httpContext);
}
