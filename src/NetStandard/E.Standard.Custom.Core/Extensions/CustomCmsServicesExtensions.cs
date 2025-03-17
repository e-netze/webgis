using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Custom.Core.Extensions;

static public class CustomCmsServicesExtensions
{
    public static CustomAuthenticationUser CheckSecurity(this IEnumerable<ICustomCmsPageSecurityService> customServices, HttpContext httpContext, string cmsId)
    {
        return customServices?.Select(c => c.CheckSecurity(httpContext, cmsId))
                              .Where(u => u != null)
                              .FirstOrDefault();
    }
}
