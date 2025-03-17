using E.Standard.Json;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.Services.Abstraction;
using E.Standard.Security.Cryptography.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace E.Standard.Security.App.Services;

public class ApplicationSecurityUserManager
{


    public ApplicationSecurityUserManager(IOptionsMonitor<ApplicationSecurityConfig> applicationSecurity,
                                          IEnumerable<IApplicationSecurityProvider> applicationSecurityProviders)
    {
        ApplicationSecurity = applicationSecurity.CurrentValue;
        ApplicationSecurityProviders = applicationSecurityProviders;
    }

    public readonly ApplicationSecurityConfig ApplicationSecurity;
    public readonly IEnumerable<IApplicationSecurityProvider> ApplicationSecurityProviders;

    //public string CurrentLoginUser(ClaimsPrincipal claimsPrincipal)
    //{
    //    if (ApplicationSecurity.UseApplicationSecurity == false)
    //    {
    //        return String.Empty;
    //    }

    //    return GetProvider().CurrentLoginUser(claimsPrincipal);
    //}

    public IApplicationSecurityProvider GetProvider()
    {
        var applicationSecurityProvider = ApplicationSecurityProviders.Where(p => p.IdentityType.Equals(ApplicationSecurity.IdentityType))
                                                                      .FirstOrDefault();

        if (applicationSecurityProvider == null)
        {
            throw new NotImplementedException($"ApplicationSecurityProvider '{ApplicationSecurity.IdentityType}' is not implemented for this application");
        }

        return applicationSecurityProvider;
    }

    public IApplicationSecurityProvider GetProviderOrDefault(IApplicationSecurityProvider defaultProvider)
    {
        var applicationSecurityProvider = ApplicationSecurityProviders.Where(p => p.IdentityType.Equals(ApplicationSecurity.IdentityType)).FirstOrDefault();

        return applicationSecurityProvider ?? defaultProvider;
    }

    #region Validations

    public bool ValidateUserPassword(ICryptoService crypto, string username, string password, string securityFilename = "")
    {
        var securityConfig = ApplicationSecurity;

        if (!String.IsNullOrWhiteSpace(securityFilename))
        {
            securityConfig = JSerializer.Deserialize<ApplicationSecurityConfig>(File.ReadAllText(securityFilename));
        }

        var user = securityConfig.Users?.Where(u => u.Name == username).FirstOrDefault();
        if (user == null)
        {
            throw new UnknownUsernameException();
        }

        if (!crypto.VerifyPassword(password, user.Password))
        {
            throw new WrongPasswordException();
        }

        return true;
    }

    public bool ValidateUsername(string username, string securityFilename = "")
    {
        var securityConfig = ApplicationSecurity;

        if (!String.IsNullOrWhiteSpace(securityFilename))
        {
            securityConfig = JSerializer.Deserialize<ApplicationSecurityConfig>(File.ReadAllText(securityFilename));
        }

        var user = securityConfig.Users?.Where(u => u.Name == username).FirstOrDefault();
        return user != null;
    }

    public ApplicationSecurityConfig.User FirstUser(string securityFilename = "")
    {
        var securityConfig = ApplicationSecurity;

        if (!String.IsNullOrWhiteSpace(securityFilename))
        {
            securityConfig = JSerializer.Deserialize<ApplicationSecurityConfig>(File.ReadAllText(securityFilename));
        }

        var user = securityConfig.Users?.FirstOrDefault();
        return user;
    }

    #endregion
}
