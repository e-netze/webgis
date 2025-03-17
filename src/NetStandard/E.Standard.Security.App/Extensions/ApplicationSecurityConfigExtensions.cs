using E.Standard.Security.App.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Security.App.Extensions;

static public class ApplicationSecurityConfigExtensions
{
    static public bool Use(this ApplicationSecurityConfig appSecurityConfig) => appSecurityConfig != null && appSecurityConfig.UseApplicationSecurity == true;

    static public bool UseOpenIdConnect(this ApplicationSecurityConfig appSecurityConfig)
        => appSecurityConfig.Use()
        && appSecurityConfig.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection
        && appSecurityConfig.OpenIdConnectConfiguration is not null;

    static public bool UseAzureAD(this ApplicationSecurityConfig appSecurityConfig)
        => appSecurityConfig.Use()
        && appSecurityConfig.IdentityType == ApplicationSecurityIdentityTypes.AzureAD
        && appSecurityConfig.AzureADConfiguration is not null;

    static public bool UseAnyOidcMethod(this ApplicationSecurityConfig appSecurityConfig)
        => appSecurityConfig.UseOpenIdConnect() || appSecurityConfig.UseAzureAD();

    static public string OidcAuthenticationUserPrefix(this ApplicationSecurityConfig appSecurityConfig) => "oidc-user";
    static public string OidcAuthenticationRolePrefix(this ApplicationSecurityConfig appSecurityConfig) => "oidc-role";

    static public ApplicationSecurityConfig.User GetUser(this ApplicationSecurityConfig securityConfig, string name)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return securityConfig?
                    .Users?
                    .Where(u => u.Name != null && u.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
    }

    static public string UseExtendedRolesFrom(this ApplicationSecurityConfig securityConfig)
        => securityConfig.UseOpenIdConnect()
           ? securityConfig.OpenIdConnectConfiguration.ExtendedRolesFrom
           : securityConfig.UseAzureAD()
             ? securityConfig.AzureADConfiguration.ExtendedRolesFrom
             : String.Empty;

    static public bool ConfirmSecurity(this ApplicationSecurityConfig securityConfig, string userName, IEnumerable<string> roles = null)
    {
        if (securityConfig == null)
        {
            return true;
        }

        switch (securityConfig.IdentityType)
        {
            case ApplicationSecurityIdentityTypes.OpenIdConnection:

                if (securityConfig.GetUser(userName) != null)
                {
                    return true;
                }
                if (roles != null &&
                   !String.IsNullOrWhiteSpace(securityConfig.OpenIdConnectConfiguration?.RequiredRole) &&
                   roles.Contains(securityConfig.OpenIdConnectConfiguration.RequiredRole))
                {
                    return true;
                }
                return false;
            case ApplicationSecurityIdentityTypes.Windows:
                if (securityConfig.GetUser(userName) != null)
                {
                    return true;
                }
                return false;
            default:
                return false;
        }
    }
}
