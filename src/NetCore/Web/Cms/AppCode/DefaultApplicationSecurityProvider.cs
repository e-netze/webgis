using Cms.AppCode.Mvc;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Services;
using E.Standard.Security.App.Services.Abstraction;
using System;
using System.Security.Claims;

namespace Cms.AppCode;

public class DefaultApplicationSecurityProvider : IApplicationSecurityProvider
{
    private readonly ApplicationSecurityUserManager _applicationSecurityUserManager;
    private readonly ApplicationSecurityController _controller;

    public DefaultApplicationSecurityProvider(
        ApplicationSecurityUserManager applicationSecurityUserManager,
        ApplicationSecurityController controller)
    {
        _controller = controller;
        _applicationSecurityUserManager = applicationSecurityUserManager;
    }

    #region IApplicationSecurityProvider

    public string IdentityType => String.Empty;

    public string CurrentLoginUser(ClaimsPrincipal claimsPrincipal)
    {
        string cookieUsername = _controller.GetCookieUsername();

        if (String.IsNullOrEmpty(cookieUsername))
        {
            throw new NotAuthorizedException();
        }

        if (!cookieUsername.StartsWith("subscriber::") && !_applicationSecurityUserManager.ValidateUsername(cookieUsername))
        {
            throw new NotAuthorizedException();
        }

        return cookieUsername;
    }

    public bool CanLogout => true;

    #endregion
}
