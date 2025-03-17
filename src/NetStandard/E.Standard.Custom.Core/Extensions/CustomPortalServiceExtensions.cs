using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.Custom.Core.Extensions;

static public class CustomPortalServiceExtensions
{
    #region ICustomPortalSecurityService

    static public void AddCustomSecurityPrefixes(this IEnumerable<ICustomPortalSecurityService> securityServices, ICollection<CustomSecurityPrefix> prefixes, IEnumerable<string> securityMethods)
    {
        if (securityServices != null && securityMethods != null)
        {
            foreach (var securityMethod in securityMethods)
            {
                foreach (var securityService in securityServices.Where(s => s.SecurityMethod.Equals(securityMethod) == true))
                {
                    foreach (var prefix in securityService.GetCustomSecurityPrefixes())
                    {
                        if (prefixes.Where(p => p.Equals(prefix)).FirstOrDefault() == null)
                        {
                            prefixes.Add(prefix);
                        }
                    }
                }
            }
        }
    }

    static public bool DisallowSubscriberUser(this IEnumerable<ICustomPortalSecurityService> securityServices, IEnumerable<string> securityMethods)
    {
        bool result = false;

        if (securityServices != null && securityMethods != null)
        {
            foreach (var securityMethod in securityMethods)
            {
                foreach (var securityService in securityServices.Where(s => s.SecurityMethod.Equals(securityMethod) == true))
                {
                    result |= securityService.DisallowSubscriberUser(securityMethod);
                }
            }
        }

        return result;
    }

    static public bool DisallowInstanceGroup(this IEnumerable<ICustomPortalSecurityService> securityServices, IEnumerable<string> securityMethods)
    {
        bool result = false;

        if (securityServices != null && securityMethods != null)
        {
            foreach (var securityMethod in securityMethods)
            {
                foreach (var securityService in securityServices.Where(s => s.SecurityMethod.Equals(securityMethod) == true))
                {
                    result |= securityService.DisallowInstanceGroup(securityMethod);
                }
            }
        }

        return result;
    }

    async static public Task<IEnumerable<string>> AutoCompleteValues(this IEnumerable<ICustomPortalSecurityService> securityServices, string term, string prefix, string cmsId = "", string subscriberId = "")
    {
        var items = new List<string>();

        if (securityServices != null)
        {
            foreach (var securityService in securityServices)
            {
                items.AddRange(await securityService.AutoCompleteValues(term, prefix, cmsId, subscriberId) ?? new string[0]);
            }
        }

        return items;
    }

    static public bool ContainsPublicUserOrClientId(this IEnumerable<ICustomPortalSecurityService> securityServices, IEnumerable<string> candidates)
    {
        return securityServices != null && securityServices.Where(c => c.ContainsPublicUserOrClientId(candidates) == true).Count() > 0;
    }

    static public bool AllowAnyUserLogin(this IEnumerable<ICustomPortalSecurityService> securityServices, IEnumerable<string> candidates)
    {
        return securityServices != null && securityServices.Where(c => c.AllowAnyUserLogin(candidates) == true).Count() > 0;
    }

    static public bool AllowUsernamesAndRolesWithWildcard(this IEnumerable<ICustomPortalSecurityService> securityServices)
    {
        return securityServices != null && securityServices.Where(c => c.AllowUsernamesAndRolesWithWildcard == true).Count() > 0;
    }

    static public string UsernameToHmacClientId(this IEnumerable<ICustomPortalSecurityService> securityServices, string username)
    {
        return securityServices?.Select(c => c.UsernameToHmacClientId(username))
                                .Where(c => !String.IsNullOrEmpty(c))
                                .FirstOrDefault();
    }

    static public CustomPortalLoginButton LoginButton(this IEnumerable<ICustomPortalSecurityService> securityServices, HttpContext httpContext, string method)
    {
        return securityServices?.Where(s => s.SecurityMethod.Equals(method))
                                .Select(s => s.LoginButton(httpContext))
                                .Where(b => !String.IsNullOrEmpty(b?.RedirectAction))
                                .FirstOrDefault();
    }

    static public (string action, string controller, object parameters)? LogoutRedirectAction(this IEnumerable<ICustomPortalSecurityService> securityServices, string portalId)
    {
        return securityServices?.Select(s => s.LogoutRedirectAction(portalId))
                                .Where(s => !String.IsNullOrEmpty(s?.action))
                                .FirstOrDefault();
    }

    #endregion

    #region ICustomPortalService

    async static public Task LogMapRequest(this IEnumerable<ICustomPortalService> customServices, string id, string category, string map, string username)
    {
        if (customServices != null)
        {
            foreach (var customService in customServices)
            {
                await customService.LogMapRequest(id, category, map, username);
            }
        }
    }

    #endregion
}
