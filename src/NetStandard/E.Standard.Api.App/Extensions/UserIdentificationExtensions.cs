using E.Standard.Api.App.Exceptions.Ogc;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core;
using E.Standard.Json;
using E.Standard.OpenIdConnect.Extensions;
using E.Standard.Security.App.Exceptions;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace E.Standard.Api.App.Extensions;

static public class UserIdentificationExtensions
{
    static public ClaimsPrincipal ToClaimsPrincipal(this CmsDocument.UserIdentification ui,
                                                    ApiAuthenticationTypes authenticationType,
                                                    string exceptionMessage = "")
    {
        List<Claim> claims = new List<Claim>();

        if (!String.IsNullOrEmpty(ui.UserId))
        {
            claims.Add(new Claim("sub", ui.UserId));
        }

        if (ui?.Userroles != null && ui.Userroles.Length > 0)
        {
            claims.Add(new Claim("role", JSerializer.Serialize(ui.Userroles)));
        }

        if (ui?.UserrolesParameters != null && ui.UserrolesParameters.Length > 0)
        {
            claims.Add(new Claim("role-parameters", JSerializer.Serialize(ui.UserrolesParameters)));
        }

        if (ui?.InstanceRoles != null && ui.InstanceRoles.Length > 0)
        {
            claims.Add(new Claim("instance-roles", JSerializer.Serialize(ui.InstanceRoles)));
        }

        if (!String.IsNullOrEmpty(ui.DisplayName))
        {
            claims.Add(new Claim("displayname", ui.DisplayName));
        }

        if (!String.IsNullOrEmpty(ui.Task))
        {
            claims.Add(new Claim("task", ui.Task));
        }

        if (!String.IsNullOrEmpty(ui.Branch))
        {
            claims.Add(new Claim("branch", ui.Branch));
        }

        if (!String.IsNullOrEmpty(exceptionMessage))
        {
            claims.Add(new Claim("exception-message", exceptionMessage));
        }

        claims.Add(new Claim("authentication-type", ((int)authenticationType).ToString()));

        var claimsIdentity = new ClaimsIdentity(new Identity(ui), claims);
        var claimsPricipal = new ClaimsPrincipal(claimsIdentity);

        return claimsPricipal;
    }

    static public CmsDocument.UserIdentification ToUserIdentification(this ClaimsPrincipal claimsPrincipal,
                                                                      ApiAuthenticationTypes acceptedAuthenticationTypes = ApiAuthenticationTypes.Any,
                                                                      bool throwExceptions = false)
    {
        if (claimsPrincipal == null)
        {
            return CmsDocument.UserIdentification.Anonymous;
        }

        var authenticationTypeClaim = claimsPrincipal.Claims.Where(c => c.Type == "authentication-type").FirstOrDefault();
        var claimAuthenticationType = authenticationTypeClaim != null ?
            (ApiAuthenticationTypes)int.Parse(authenticationTypeClaim.Value) :
            ApiAuthenticationTypes.Unknown;

        if (throwExceptions)
        {
            var exceptionsClaim = claimsPrincipal.Claims.Where(c => c.Type == "exception-message").FirstOrDefault();
            if (exceptionsClaim != null && !String.IsNullOrEmpty(exceptionsClaim.Value))
            {
                switch (claimAuthenticationType)
                {
                    case ApiAuthenticationTypes.CustomOgcTicket:
                        throw new OgcNotAuthorizedException(exceptionsClaim.Value);
                    default:
                        throw new NotAuthorizedException(exceptionsClaim.Value);
                }
            }
        }

        if (acceptedAuthenticationTypes != ApiAuthenticationTypes.Any)
        {
            if (!acceptedAuthenticationTypes.HasFlag(claimAuthenticationType))
            {
                return CmsDocument.UserIdentification.Anonymous;
            }
        }

        var rolesParametersClaim = claimsPrincipal.Claims.Where(c => c.Type == "role-parameters").FirstOrDefault();
        var instanceRolesClaim = claimsPrincipal.Claims.Where(c => c.Type == "instance-roles").FirstOrDefault();
        var displayNameClaim = claimsPrincipal.Claims.Where(c => c.Type == "displayname").FirstOrDefault();
        var taskClaim = claimsPrincipal.Claims.Where(c => c.Type == "task").FirstOrDefault();
        var branchClaim = claimsPrincipal.Claims.Where(c => c.Type == "branch").FirstOrDefault();

        var ui = new CmsDocument.UserIdentification(
            claimsPrincipal.GetUsername(),
            claimsPrincipal.GetRoles()?.ToArray(),
            rolesParametersClaim != null ? JSerializer.Deserialize<string[]>(rolesParametersClaim.Value) : null,
            instanceRolesClaim != null ? JSerializer.Deserialize<string[]>(instanceRolesClaim.Value) : null,
            userId: claimsPrincipal.GetUserId(),
            displayName: displayNameClaim?.Value,
            task: taskClaim?.Value,
            branch: branchClaim?.Value
            );

        return ui;
    }

    static public bool IsAuthenticatedApiUser(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.Identity != null &&
               claimsPrincipal.Identity.IsAuthenticated &&
               claimsPrincipal.ApiAuthenticationType() != ApiAuthenticationTypes.Unknown;
    }

    static public ApiAuthenticationTypes ApiAuthenticationType(this ClaimsPrincipal claimsPrincipal)
    {
        var authenticationTypeClaim = claimsPrincipal.Claims.Where(c => c.Type == "authentication-type").FirstOrDefault();
        var claimAuthenticationType = authenticationTypeClaim != null ? (ApiAuthenticationTypes)int.Parse(authenticationTypeClaim.Value) :
                                                                         ApiAuthenticationTypes.Unknown;

        return claimAuthenticationType;
    }

    static public bool RequestsBranch(this CmsDocument.UserIdentification ui)
        => !string.IsNullOrEmpty(ui?.Branch);

    #region Helper Class

    private class Identity : IIdentity
    {
        public Identity(CmsDocument.UserIdentification ui)
        {
            this.Name = ui?.Username;
            _isAuthenticated = ui != null && !ui.IsAnonymous;

            if (_isAuthenticated)
            {
                _authenticationType = "AuthenticationTypes.Federation";
            }
        }

        private readonly string _authenticationType;
        public string AuthenticationType => _authenticationType;

        private readonly bool _isAuthenticated = false;
        public bool IsAuthenticated => _isAuthenticated;

        public string Name { get; }
    }

    #endregion
}
