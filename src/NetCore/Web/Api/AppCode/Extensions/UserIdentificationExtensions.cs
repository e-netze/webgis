using Api.Core.AppCode.Services;
using E.DataLinq.Web.Reflection;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Reflection;
using E.Standard.Custom.Core;
using System.Security.Claims;

namespace Api.Core.AppCode.Extensions;

static public class UserIdentificationExtensions
{
    static public bool ApplyAuthenticationMiddleware(this ClaimsPrincipal claimsPrincipal, RoutingEndPointReflectionService endpointReflection, ApiAuthenticationTypes authType)
    {
        if (claimsPrincipal.IsAuthenticatedApiUser())
        {
            return false;
        }

        var apiAuthenticationAttribute = endpointReflection.GetCustomAttribute<ApiAuthenticationAttribute>();

        return apiAuthenticationAttribute != null &&
               apiAuthenticationAttribute.AuthenticationTypes.HasFlag(authType);
    }

    static public bool ApplyDataLinqHostAuthentication(this ClaimsPrincipal claimsPrincipal, RoutingEndPointReflectionService endpointReflection, HostAuthenticationTypes authType)
    {
        if (claimsPrincipal.IsAuthenticatedApiUser())
        {
            return false;
        }

        var hostAuthenticationAttriute = endpointReflection.GetCustomAttribute<HostAuthenticationAttribute>();

        return hostAuthenticationAttriute != null &&
               hostAuthenticationAttriute.AuthenticationType.HasFlag(authType);
    }
}
