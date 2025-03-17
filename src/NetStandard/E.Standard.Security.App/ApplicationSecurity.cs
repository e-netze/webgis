using E.Standard.Security.App.Reflection;
using E.Standard.Security.App.Services;
using E.Standard.Security.App.Services.Abstraction;
using System;
using System.Reflection;
using System.Security.Claims;

namespace E.Standard.Security.App;

public class ApplicationSecurity
{
    private readonly ApplicationSecurityUserManager _applicationSecurityUserManager;
    private readonly IApplicationSecurityProvider _defaultApplicationSecurityProvider;

    public ApplicationSecurity(ApplicationSecurityUserManager applicationSecurityUserManager, IApplicationSecurityProvider defautApplicationSecurityProvider)
    {
        _applicationSecurityUserManager = applicationSecurityUserManager;
        _defaultApplicationSecurityProvider = defautApplicationSecurityProvider;
    }

    public string CheckSecurity(
        object instance,
        MethodInfo methodInfo,
        ClaimsPrincipal claimsPrincipal)
    {
        var applicationSecrurityAttribute =
            instance?.GetType().GetCustomAttribute<ApplicationSecurityAttribute>() ??
            methodInfo?.GetCustomAttribute<ApplicationSecurityAttribute>();

        if (applicationSecrurityAttribute != null && applicationSecrurityAttribute.CheckSecurity == true)
        {
            return CurrentLoginUser(claimsPrincipal);
        }

        return String.Empty;
    }

    public string CurrentLoginUser(ClaimsPrincipal claimsPrincipal)
    {
        if (_applicationSecurityUserManager?.ApplicationSecurity == null ||
            _applicationSecurityUserManager.ApplicationSecurity.UseApplicationSecurity == false)
        {
            return String.Empty;
        }

        var applicationSecurityProvider = _defaultApplicationSecurityProvider == null ?
            _applicationSecurityUserManager.GetProvider() :
            _applicationSecurityUserManager.GetProviderOrDefault(_defaultApplicationSecurityProvider);

        return applicationSecurityProvider.CurrentLoginUser(claimsPrincipal);
    }

    public bool CanLogout
    {
        get
        {
            if (_applicationSecurityUserManager?.ApplicationSecurity == null ||
                _applicationSecurityUserManager.ApplicationSecurity.UseApplicationSecurity == false)
            {
                return false;
            }

            var applicationSecurityProvider = _defaultApplicationSecurityProvider == null ?
                _applicationSecurityUserManager.GetProvider() :
                _applicationSecurityUserManager.GetProviderOrDefault(_defaultApplicationSecurityProvider);

            return applicationSecurityProvider.CanLogout;
        }
    }
}
