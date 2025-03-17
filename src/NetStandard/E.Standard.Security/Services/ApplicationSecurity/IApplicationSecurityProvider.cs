using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace E.Standard.Security.Services.ApplicationSecurity
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    public interface IApplicationSecurityProvider
    {
        string IdentityType { get; }
        string CurrentLoginUser(ClaimsPrincipal claimsPrincipal);

        bool CanLogout { get; }
    }
}
