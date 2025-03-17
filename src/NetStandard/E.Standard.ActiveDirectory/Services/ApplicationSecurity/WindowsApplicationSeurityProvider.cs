using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace E.Standard.ActiveDirectory.Services.ApplicationSecurity;

public class WindowsApplicationSeurityProvider : IApplicationSecurityProvider
{
    private readonly ApplicationSecurityConfig _applicationSecurity;
    public WindowsApplicationSeurityProvider(IOptionsMonitor<ApplicationSecurityConfig> applicationSecurity)
    {
        _applicationSecurity = applicationSecurity.CurrentValue;
    }

    #region IApplicationSecurityProvider

    public string IdentityType => ApplicationSecurityIdentityTypes.Windows;

    public string CurrentLoginUser(ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal?.Identity == null ||
            claimsPrincipal.Identity.IsAuthenticated == false ||
            _applicationSecurity.ConfirmSecurity(claimsPrincipal.Identity.Name) == false)
        {
            throw new NotAuthorizedException(claimsPrincipal);
        }
        return claimsPrincipal.Identity?.Name ?? "";
    }

    public bool CanLogout => false;

    #endregion
}
