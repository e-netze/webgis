using E.Standard.Security.Exceptions;
using E.Standard.Security.Json;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace E.Standard.Security.Services.ApplicationSecurity
{
    [Obsolete("Use E.Standard.Security.App assembly")]
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

        public string CurrentLoginUser(ClaimsPrincipal claimsPrincipal)
        {
            if (ApplicationSecurity.UseApplicationSecurity == false)
            {
                return String.Empty;
            }

            return GetProvider().CurrentLoginUser(claimsPrincipal);
        }

        public IApplicationSecurityProvider GetProvider()
        {
            var applicationSecurityProvider = ApplicationSecurityProviders.Where(p => p.IdentityType.Equals(ApplicationSecurity.IdentityType))
                                                                          .FirstOrDefault();

            if (applicationSecurityProvider == null)
            {
                throw new NotImplementedException($"ApplicationSecurityProvider '{ ApplicationSecurity.IdentityType }' is not implemented for this application");
            }

            return applicationSecurityProvider;
        }

        public IApplicationSecurityProvider GetProviderOrDefault(IApplicationSecurityProvider defaultProvider)
        {
            var applicationSecurityProvider = ApplicationSecurityProviders.Where(p => p.IdentityType.Equals(ApplicationSecurity.IdentityType)).FirstOrDefault();

            return applicationSecurityProvider ?? defaultProvider;
        }

        #region Validations

        public bool ValidateUserPassword(string username, string password, string securityFilename = "")
        {
            var securityConfig = ApplicationSecurity;

            if (!String.IsNullOrWhiteSpace(securityFilename))
            {
                securityConfig = JsonConvert.DeserializeObject<ApplicationSecurityConfig>(File.ReadAllText(securityFilename));
            }

            var user = securityConfig.Users?.Where(u => u.Name == username).FirstOrDefault();
            if (user == null)
            {
                throw new UnknownUsernameException();
            }

            if (!new Crypto().VerifyPassword(password, user.Password))
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
                securityConfig = JsonConvert.DeserializeObject<ApplicationSecurityConfig>(File.ReadAllText(securityFilename));
            }

            var user = securityConfig.Users?.Where(u => u.Name == username).FirstOrDefault();
            return user != null;
        }

        public ApplicationSecurityConfig.User FirstUser(string securityFilename = "")
        {
            var securityConfig = ApplicationSecurity;

            if (!String.IsNullOrWhiteSpace(securityFilename))
            {
                securityConfig = JsonConvert.DeserializeObject<ApplicationSecurityConfig>(File.ReadAllText(securityFilename));
            }

            var user = securityConfig.Users?.FirstOrDefault();
            return user;
        }

        #endregion
    }
}
