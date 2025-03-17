#nullable enable

using E.Standard.Json;
using E.Standard.Security.App.Json;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace E.Standard.OpenIdConnect.Extensions;

static public class OidcExtenstions
{
    private const string DefaultRolesClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    static public IEnumerable<string> GetRoles(this ClaimsPrincipal claimsPrincipal, ApplicationSecurityConfig? appSecurity = null)
    {
        if (claimsPrincipal?.Claims == null)
        {
            return new string[0];
        }

        string rolesClaimType = appSecurity.RoleClaimType();

        var roles = claimsPrincipal
                .Claims
                .Where(c => c.Type == rolesClaimType)
                .Select(c => c.Value)
                .ToArray();

        if (roles == null || roles.Length == 0)
        {
            var roleClaim = claimsPrincipal
                  .Claims
                  .Where(c => c.Type == "role")
                  .FirstOrDefault();

            if (roleClaim != null && roleClaim.Value != null && roleClaim.Value.StartsWith("["))
            {
                try
                {
                    return JSerializer.Deserialize<string[]>(roleClaim.Value)!;
                }
                catch { }
            }
        }

        return roles!;
    }

    static public IEnumerable<string> GetRoleParameters(this ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Claims != null)
        {
            var roleClaim = claimsPrincipal
                      .Claims
                      .Where(c => c.Type == "role-parameters")
                      .FirstOrDefault();

            if (roleClaim != null && roleClaim.Value != null && roleClaim.Value.StartsWith("["))
            {
                try
                {
                    return JSerializer.Deserialize<string[]>(roleClaim.Value)!;
                }
                catch { }
            }
        }
        return new string[0];
    }

    static public string GetEmail(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?
                            .Claims?
                            .Where(c => c.Type == "email" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                            .FirstOrDefault()?
                            .Value ?? "";
    }

    static public string GetUsername(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal.Identity?.Name ?? "";
    }

    static public string TryGetUsername(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.Identity?.Name ?? "";
    }

    static public string GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?
                    .Claims?
                    .Where(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" || c.Type == "sub")
                    .FirstOrDefault()?
                    .Value ?? "";
    }

    static public string[] ConcatRoles(this string[] roles1, string[] roles2)
    {
        if (roles1 == null || roles1.Length == 0)
        {
            return roles2 ?? new string[0];
        }

        if (roles2 == null || roles2.Length == 0)
        {
            return roles1 ?? new string[0];
        }

        var roles = new string[roles1.Length + roles2.Length];
        roles1.CopyTo(roles, 0);
        roles2.CopyTo(roles, roles1.Length);

        return roles;
    }

    static private string RoleClaimType(this ApplicationSecurityConfig? appSecurity)
    {
        if (appSecurity?.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection)
        {
            if (!string.IsNullOrEmpty(appSecurity.OpenIdConnectConfiguration?.RoleClaimType))
            {
                return appSecurity.OpenIdConnectConfiguration.RoleClaimType;
            }
        }

        return DefaultRolesClaimType;
    }
}
