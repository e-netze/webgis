using System.Threading.Tasks;

using E.Standard.Custom.Core.Models;
using E.Standard.WebGIS.Core;

using Microsoft.AspNetCore.Http;

namespace E.Standard.Custom.Core.Abstractions;

public interface IPortalAuthenticationService
{
    Task<PortalAuthenticationServiceUser> TryAuthenticationServiceUser(HttpContext httpContext, string username, bool cache = false);

    UserType UserType { get; }
}
