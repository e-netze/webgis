using E.Standard.Custom.Core.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace E.Standard.Custom.Core.Abstractions;

public interface ICustomPortalSecurityService
{
    string SecurityMethod { get; }
    IEnumerable<CustomSecurityPrefix> GetCustomSecurityPrefixes();

    Task<IEnumerable<string>> AutoCompleteValues(string term, string prefix, string cmsId = "", string subscriberId = "");

    bool DisallowSubscriberUser(string securityMethod);
    bool DisallowInstanceGroup(string securityMethod);

    bool ContainsPublicUserOrClientId(IEnumerable<string> candidates);
    bool AllowAnyUserLogin(IEnumerable<string> candidates);

    bool AllowUsernamesAndRolesWithWildcard { get; }

    string UsernameToHmacClientId(string username);

    (string action, string controller, object parameters)? LogoutRedirectAction(string portalId);

    CustomPortalLoginButton LoginButton(HttpContext context);
}
