using System;

namespace E.Standard.Custom.Core;

[Flags]
public enum WebGISAppliationTarget
{
    Api = 1,
    Portal = 2,
    Cms = 4
}

[Flags]
public enum ApiAuthenticationTypes
{
    Any = 0,
    Unknown = 1,
    Cookie = 2,
    Hmac = 4,
    CustomOgcTicket = 8,
    PortalProxyRequest = 16,
    ClientIdAndSecret = 32,
    CustomAuthentication2 = 128,
    CustomAuthentication3 = 256,
    CustomAuthentication4 = 512,
    CustomAccessToken = 1024,
    BearerAccessToken = 2048,
    BasicAuthentication = 4096
}
