using E.Standard.Custom.Core.Abstractions;
using E.Standard.WebGIS.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Portal.Core.Models.Auth;

public class AuthPortalModel
{
    private static readonly string[] KnownAuthenticationMethots = new string[] { "oidc" };

    public AuthPortalModel(ApiPortalPageDTO portal,
                           bool isAuthorized,
                           IEnumerable<string> allowedSecurtyMethods,
                           IEnumerable<ICustomPortalSecurityService> customSecurityServices)
    {
        this.Portal = portal;
        this.IsAuthorized = isAuthorized;
        this.AllowedAuthenticationMethods = new List<string>();

        if (allowedSecurtyMethods.Contains("oidc") &&
           (HasPrefix(Portal.Users, "oidc-user::") || HasPrefix(Portal.Users, "oidc-role::")))
        {
            this.AllowedAuthenticationMethods.Add("oidc");
        }

        if (customSecurityServices != null)
        {
            foreach (var customSecurityService in customSecurityServices)
            {
                foreach (var allowedAuthenticationMetehod in allowedSecurtyMethods.Where(m => !KnownAuthenticationMethots.Contains(m)))
                {
                    if (customSecurityService.SecurityMethod.Equals(allowedAuthenticationMetehod))
                    {
                        if (customSecurityService.GetCustomSecurityPrefixes().Where(p => HasPrefix(Portal.Users, p.name)).Count() > 0)
                        {
                            AllowedAuthenticationMethods.Add(allowedAuthenticationMetehod);
                        }
                    }
                }
            }
        }
    }

    #region Properties

    private ApiPortalPageDTO Portal { get; set; }

    public string Id { get { return this.Portal.Id; } }
    public string Name { get { return Portal.Name; } }
    public string Description { get { return Portal.Description; } }

    public ICollection<string> AllowedAuthenticationMethods { get; }

    public bool AllowSubscriberLogin
    {
        get
        {
            return true || HasPrefix(Portal.Users, "subscriber::");  // Subscriber geht immer
        }
    }

    public bool IsAuthorized { get; set; }

    #endregion

    #region Helper

    private bool HasPrefix(string[] list, string prefix)
    {
        if (list == null)
        {
            return false;
        }

        foreach (var l in list)
        {
            if (l.StartsWith(prefix))
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}
