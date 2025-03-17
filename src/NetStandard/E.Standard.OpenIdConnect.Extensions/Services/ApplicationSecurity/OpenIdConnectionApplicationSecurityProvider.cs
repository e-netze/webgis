using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace E.Standard.OpenIdConnect.Extensions.Services.ApplicationSecurity;

public class OpenIdConnectionApplicationSecurityProvider : IApplicationSecurityProvider
{
    private readonly ApplicationSecurityConfig _applicationSecurity;
    public OpenIdConnectionApplicationSecurityProvider(IOptionsMonitor<ApplicationSecurityConfig> applicationSecurity)
    {
        _applicationSecurity = applicationSecurity.CurrentValue;
    }

    #region IApplicationSecurityProvider

    public string IdentityType => ApplicationSecurityIdentityTypes.OpenIdConnection;

    public string CurrentLoginUser(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Identity == null ||
            claimsPrincipal.Identity.IsAuthenticated == false ||
            _applicationSecurity.ConfirmSecurity(
                        claimsPrincipal.GetUsername(),
                        claimsPrincipal.GetRoles(_applicationSecurity)) == false)
        {
            throw new NotAuthorizedException(claimsPrincipal);
        }

        return claimsPrincipal.GetUsername();
    }

    public bool CanLogout => true;

    #endregion
}
