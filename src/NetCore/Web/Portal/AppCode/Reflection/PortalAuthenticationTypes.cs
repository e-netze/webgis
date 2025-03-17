using System;

namespace Portal.Core.AppCode.Reflection;

[Flags]
public enum PortalAuthenticationTypes
{
    Any = 0,
    Unknown = 1,
    Cookie = 2
}
