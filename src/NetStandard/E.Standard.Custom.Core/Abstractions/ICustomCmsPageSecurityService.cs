using E.Standard.Custom.Core.Models;
using Microsoft.AspNetCore.Http;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomCmsPageSecurityService
{
    CustomAuthenticationUser CheckSecurity(HttpContext httpContext, string cmsId);
}
