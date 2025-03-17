using E.Standard.ActiveDirectory;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using E.Standard.WebGIS.Core;
using Microsoft.AspNetCore.Http;
using Portal.Core.AppCode.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services.Authentication;

public class WindowsCustomPortalSecurityService : ICustomPortalSecurityService
{
    private readonly ConfigurationService _config;

    public WindowsCustomPortalSecurityService(ConfigurationService config)
    {
        _config = config;
    }

    public string SecurityMethod => "windows";

    public bool DisallowSubscriberUser(string securityMethod) => false;

    public bool DisallowInstanceGroup(string securityMethod) => false;

    public IEnumerable<CustomSecurityPrefix> GetCustomSecurityPrefixes()
    {
        return new CustomSecurityPrefix[] {
            new CustomSecurityPrefix("nt-user::", "user"),
            new CustomSecurityPrefix("nt-group::", "group")
        };
    }

    public Task<IEnumerable<string>> AutoCompleteValues(string term, string prefix, string cmsId = "", string subscriberId = "")
    {
        List<string> items = new List<string>();

        if (String.IsNullOrEmpty(prefix) || prefix == "nt-user::" || prefix == "nt-group::")
        {
            if (String.IsNullOrWhiteSpace(term) || term.Trim().Length < 3)
            {
                items.Add("Type min 3 letters...");
            }
            else
            {
                try
                {
                    var adQuery = ActiveDirectoryFactory.InterfaceImplementation<IAdQuery>(_config.SecurityWindowsAuthenticationLdapDirectory());
                    foreach (var adObject in adQuery.FindAdObjects($"{term}*"))
                    {
                        if ((String.IsNullOrEmpty(prefix) || prefix == "nt-group::") && adObject is AdGroup)
                        {
                            items.Add(!String.IsNullOrEmpty(prefix) ?
                                        ((AdGroup)adObject).Groupname :
                                        UserManagement.AppendUserPrefix(((AdGroup)adObject).Groupname, UserType.WindowsGroup));
                        }
                        else if ((String.IsNullOrEmpty(prefix) || prefix == "nt-user::") && adObject is AdUser)
                        {
                            string format = _config.SecurityWindowsAuthenticationLdapFormat();
                            string userName = ((AdUser)adObject).Username;
                            if (format.Contains("{0}"))
                            {
                                userName = String.Format(format, userName);
                            }

                            items.Add(!String.IsNullOrEmpty(prefix) ?
                                        userName :
                                        UserManagement.AppendUserPrefix(userName, UserType.WindowsUser));
                        }
                    }
                }
                catch (Exception ex)
                {
                    items.Add("nt-exception::" + ex.Message);
                }
            }
        }

        return Task.FromResult<IEnumerable<string>>(items);
    }

    public bool ContainsPublicUserOrClientId(IEnumerable<string> candidates) => false;
    public bool AllowAnyUserLogin(IEnumerable<string> candidates) => false;

    public bool AllowUsernamesAndRolesWithWildcard => false;

    public string UsernameToHmacClientId(string username) => String.Empty;

    public CustomPortalLoginButton LoginButton(HttpContext context)
    {
        return new CustomPortalLoginButton()
        {
            Method = this.SecurityMethod,
            Title = "Anmelden (Login)",
            Description = "Mit Windows Domänen Account am Portal anmelden",
            RelativeImagePath = "/content/img/login/login-100-w.png",
            RedirectAction = "LoginAD"
        };
    }

    public (string action, string controller, object parameters)? LogoutRedirectAction(string portalId) => null;
}
