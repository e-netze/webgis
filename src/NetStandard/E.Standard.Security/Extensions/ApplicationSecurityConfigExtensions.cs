using E.Standard.Security.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Extensions
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    static public class ApplicationSecurityConfigExtensions
    {
        static public bool Use(this ApplicationSecurityConfig appSecurityConfig) => appSecurityConfig != null && appSecurityConfig.UseApplicationSecurity == true;

        static public bool UseOpenIdConnect(this ApplicationSecurityConfig appSecurityConfig)
            => appSecurityConfig.Use() && appSecurityConfig.IdentityType == ApplicationSecurityIdentityTypes.OpenIdConnection;
    }
}
