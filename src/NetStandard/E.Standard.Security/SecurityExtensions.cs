using E.Standard.Security.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.Security
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    static public class SecurityExtensions
    {
        static public  ApplicationSecurityConfig.User GetUser(this ApplicationSecurityConfig securityConfig , string name)
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

        static public bool ConfirmSecurity(this ApplicationSecurityConfig securityConfig, string userName, IEnumerable<string> roles=null)
        {
            if (securityConfig == null)
            {
                return true;
            }

            switch (securityConfig.IdentityType)
            {
                case ApplicationSecurityIdentityTypes.OpenIdConnection:
                    
                    if(securityConfig.GetUser(userName)!=null)
                    {
                        return true;
                    }
                    if(roles!=null &&
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
}
