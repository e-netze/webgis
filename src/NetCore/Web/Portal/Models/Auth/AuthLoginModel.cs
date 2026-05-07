using System.Collections.Generic;

using E.Standard.Custom.Core.Models;

namespace Portal.Core.Models.Auth;

public class AuthLoginModel
{
    public string PortalId { get; set; }
    public string SubscriberLoginUrl { get; set; }

    public AuthPortalModel[] Portals { get; set; }

    public string CurrentUsername { get; set; }

    public IEnumerable<CustomPortalLoginButton> LoginButtons { get; set; }
}
