namespace E.Standard.Api.App.Configuration;

public enum CookieSecureMode
{
    /// <summary>Secure flag only if the request is HTTPS (incl. X-Forwarded-Proto). Allows http://localhost.</summary>
    Auto,
    Always,
    Never
}

/// <summary>
/// Security options for the subscriber (forms) login and the API auth cookie.
/// </summary>
/// <remarks>
/// Configurable via <c>api.config</c>, section <c>security</c>
/// (bound by <c>ConfigurationServiceExtensions.BindSubscriberSecurityOptions</c>):
/// <list type="bullet">
/// <item><c>security:cookie-secure</c> (auto|always|never) → <see cref="CookieSecure"/></item>
/// <item><c>security:session-max-lifetime-minutes</c> → <see cref="SessionMaxLifetimeMinutes"/></item>
/// <item><c>security:session-idle-timeout-minutes</c> → <see cref="SessionIdleTimeoutMinutes"/></item>
/// <item><c>security:password-min-length</c> → <see cref="PasswordMinLength"/></item>
/// </list>
/// </remarks>
public class SubscriberSecurityOptions
{
    public const int PasswordMaxLength = 128;

    public CookieSecureMode CookieSecure { get; set; } = CookieSecureMode.Auto;

    /// <summary>
    /// Absolute lifetime of an auth cookie session, regardless of activity. 0 = unlimited (not recommended).
    /// </summary>
    public int SessionMaxLifetimeMinutes { get; set; } = 480;

    /// <summary>
    /// Session ends after this period without requests. 0 = disabled.
    /// </summary>
    public int SessionIdleTimeoutMinutes { get; set; } = 0;

    /// <summary>
    /// Minimum length for new or changed subscriber passwords (existing passwords stay valid).
    /// </summary>
    public int PasswordMinLength { get; set; } = 12;
}
