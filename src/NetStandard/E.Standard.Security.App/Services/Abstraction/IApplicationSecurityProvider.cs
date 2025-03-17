using System.Security.Claims;

namespace E.Standard.Security.App.Services.Abstraction;

public interface IApplicationSecurityProvider
{
    string IdentityType { get; }
    string CurrentLoginUser(ClaimsPrincipal claimsPrincipal);

    bool CanLogout { get; }
}
