#nullable enable

using E.Standard.Custom.Core.Models;
using E.Standard.Json;
using E.Standard.OpenIdConnect.Extensions;
using E.Standard.Security.App.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Portal.Core.AppCode.Extensions;


static public class PortalUserExtensions
{
    private const string DisplayNameClaimType = "displayname";
    private const string IsPortalUserClaimType = "is_portaluser";
    private const string StopAuthenticationPropagationClaimType = "stop_auth_propagation";

    static public PortalUser? ToPortalUser(this PortalAuthenticationServiceUser authenticationServiceUser)
    {
        if (authenticationServiceUser == null)
        {
            return null;
        }

        return new PortalUser(
            authenticationServiceUser.Username,
            authenticationServiceUser.UserRoles,
            null,
            authenticationServiceUser.DisplayName);
    }

    static public ClaimsPrincipal ToClaimsPricipal(this PortalUser portalUser, bool stopAuthenicationMiddlewarePropagation = true)
    {
        List<Claim> claims = new List<Claim>();

        if (portalUser?.UserRoles != null && portalUser.UserRoles.Length > 0)
        {
            claims.Add(new Claim("role", JSerializer.Serialize(portalUser.UserRoles)));
        }
        if (portalUser?.RoleParameters != null && portalUser.RoleParameters.Length > 0)
        {
            claims.Add(new Claim("role-parameters", JSerializer.Serialize(portalUser.RoleParameters)));
        }

        if (stopAuthenicationMiddlewarePropagation)
        {
            claims.Add(new Claim(StopAuthenticationPropagationClaimType, "1"));
        }
        claims.Add(new Claim(IsPortalUserClaimType, "1"));

        if (!String.IsNullOrWhiteSpace(portalUser?.DisplayName))
        {
            claims.Add(new Claim(DisplayNameClaimType, portalUser.DisplayName));
        }

        var claimsIdentity = new ClaimsIdentity(new Identity(portalUser), claims);
        var claimsPricipal = new ClaimsPrincipal(claimsIdentity);

        return claimsPricipal;
    }

    static public PortalUser ToPortalUser(this ClaimsPrincipal claimsPrincipal, ApplicationSecurityConfig? appSecurityConfig)
    {
        if (claimsPrincipal == null)
        {
            return new PortalUser(String.Empty);
        }

        var portalUser = new PortalUser(
            claimsPrincipal.GetUsername(),
            claimsPrincipal.GetRoles(appSecurityConfig)?.ToArray(),
            claimsPrincipal.GetRoleParameters()?.ToArray(),
            claimsPrincipal?
                    .Claims?
                    .Where(c => c.Type == DisplayNameClaimType)
                    .FirstOrDefault()?
                    .Value ?? String.Empty
            );

        return portalUser;
    }

    static public bool StopAuthenticationPropagation(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?
                    .Claims?
                    .Where(c => c.Type == StopAuthenticationPropagationClaimType)
                    .FirstOrDefault()?
                    .Value == "1";
    }

    static public bool IsPortalUserClaimsPrincipal(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?
                    .Claims?
                    .Where(c => c.Type == IsPortalUserClaimType)
                    .FirstOrDefault()?
                    .Value == "1";
    }

    static public bool ApplyAuthenticationMiddleware(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal == null ||
               !(claimsPrincipal.Identity?.IsAuthenticated == true && claimsPrincipal.StopAuthenticationPropagation());
    }

    static public bool HasUsernamePrefix(this PortalUser portalUser)
    {
        return portalUser?.Username != null ? portalUser.Username.Contains("::") : false;
    }

    static public bool HasUsernamePrefix(this PortalUser portalUser, string prefix)
        => portalUser.HasUsernamePrefix() && portalUser.Username.StartsWith(prefix);

    static public PortalUser SetPrefixes(this PortalUser portalUser, string usernamePrefix, string rolePrefix)
    {
        if (portalUser.HasUsernamePrefix())
        {
            throw new Exception("User already has a prefix");
        }

        if (!String.IsNullOrEmpty(portalUser?.Username))
        {
            var roles = portalUser.UserRoles?
                                  .Select(r => r.Contains("::") ? r : $"{rolePrefix}::{r}")
                                  .ToArray();

            return new PortalUser(
                $"{usernamePrefix}::{portalUser.Username}",
                roles,
                portalUser.RoleParameters,
                portalUser.DisplayName == portalUser.Username ? $"{usernamePrefix}::{portalUser.Username}" : portalUser.DisplayName);
        }

        return portalUser!;
    }

    static public PortalUser? AppendRoles(this PortalUser portalUser, IEnumerable<string> roles)
    {
        if (portalUser == null || portalUser.IsAnonymous)
        {
            return portalUser;
        }

        var userRoles = new List<string>();

        if (portalUser.UserRoles != null)
        {
            userRoles.AddRange(portalUser.UserRoles);
        }

        if (roles != null)
        {
            userRoles.AddRange(roles);
        }

        return new PortalUser(
            portalUser.Username,
            userRoles.ToArray(),
            portalUser.RoleParameters,
            portalUser.DisplayName);
    }

    #region Helper Class

    private class Identity : IIdentity
    {
        public Identity(PortalUser? portalUser)
        {
            this.Name = portalUser?.Username!;
            _isAuthenticated = portalUser != null && !portalUser.IsAnonymous;

            if (_isAuthenticated)
            {
                _authenticationType = "AuthenticationTypes.Federation";
            }
        }

        private readonly string? _authenticationType;
        public string? AuthenticationType => _authenticationType;

        private readonly bool _isAuthenticated = false;
        public bool IsAuthenticated => _isAuthenticated;

        public string Name { get; }
    }

    #endregion
}
