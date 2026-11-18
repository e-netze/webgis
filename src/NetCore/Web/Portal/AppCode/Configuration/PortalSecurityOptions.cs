namespace Portal.Core.AppCode.Configuration;

public class PortalSecurityOptions
{
    // Auth cookies written by older versions (AES-CBC without integrity protection) are rejected by default.
    public bool AcceptLegacyAuthCookie { get; set; } = false;
}
