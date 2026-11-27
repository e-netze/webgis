namespace Portal.Core.AppCode.Configuration;

public enum PortalCookieSecureMode
{
    Auto,
    Always,
    Never
}

public class PortalSecurityOptions
{
    // Secure flag of the auth cookie: Auto = only for https requests.
    public PortalCookieSecureMode CookieSecure { get; set; } = PortalCookieSecureMode.Auto;

    // Auth cookies written by older versions (AES-CBC without integrity protection) are rejected by default.
    public bool AcceptLegacyAuthCookie { get; set; } = false;

    // Hosts (e.g. "reports.example.com" or "*.example.com") a login redirect (webgis-redirect) may point to,
    // in addition to the portal and api host, which are always allowed.
    public string[] AllowedRedirectHosts { get; set; } = [];
}
