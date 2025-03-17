using System;

namespace Portal.Core.AppCode.Reflection;

public class PortalAuthenticationAttribute : Attribute
{
    public PortalAuthenticationAttribute(PortalAuthenticationTypes authenticationTypes)
    {
        AuthenticationTypes = authenticationTypes;
    }

    public PortalAuthenticationTypes AuthenticationTypes { get; }
}
