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
/// <item><c>security:login-captcha-after-failed-attempts</c> → <see cref="LoginCaptchaAfterFailedAttempts"/></item>
/// <item><c>security:login-lockout-after-failed-attempts</c> → <see cref="LoginLockoutAfterFailedAttempts"/></item>
/// <item><c>security:login-lockout-minutes</c> (e.g. "1,5,15") → <see cref="LoginLockoutMinutes"/></item>
/// <item><c>security:login-failed-attempts-reset-minutes</c> → <see cref="LoginFailedAttemptsResetMinutes"/></item>
/// <item><c>security:login-ip-max-failed-attempts</c> → <see cref="LoginIpMaxFailedAttempts"/></item>
/// <item><c>security:login-ip-window-minutes</c> → <see cref="LoginIpWindowMinutes"/></item>
/// <item><c>security:login-throttle-redis-connectionstring</c> → <see cref="LoginThrottleRedisConnectionString"/></item>
/// <item><c>security:captcha-lifetime-minutes</c> → <see cref="CaptchaLifetimeMinutes"/></item>
/// <item><c>security:password-verification-cache-seconds</c> → <see cref="PasswordVerificationCacheSeconds"/></item>
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

    /// <summary>
    /// Failed logins (per username or client IP) after which a captcha is required. 0 = always.
    /// </summary>
    public int LoginCaptchaAfterFailedAttempts { get; set; } = 3;

    /// <summary>
    /// Failed logins per username after which the username is temporarily locked. 0 = no lockout.
    /// </summary>
    public int LoginLockoutAfterFailedAttempts { get; set; } = 10;

    /// <summary>
    /// Escalating lockout durations; each further failed login after the lockout threshold uses the next step (last one repeats).
    /// </summary>
    public int[] LoginLockoutMinutes { get; set; } = [1, 5, 15];

    /// <summary>
    /// The failed login counter of a username is reset after this period without further failures.
    /// </summary>
    public int LoginFailedAttemptsResetMinutes { get; set; } = 60;

    /// <summary>
    /// Failed logins per client IP within <see cref="LoginIpWindowMinutes"/> after which every login
    /// from this IP requires a captcha and is slowed down (never a hard block). 0 = disabled.
    /// </summary>
    public int LoginIpMaxFailedAttempts { get; set; } = 30;

    public int LoginIpWindowMinutes { get; set; } = 15;

    /// <summary>
    /// Optional: share login throttling state between instances via Redis. Empty = in-memory (per instance).
    /// </summary>
    public string LoginThrottleRedisConnectionString { get; set; } = "";

    public int CaptchaLifetimeMinutes { get; set; } = 5;

    /// <summary>
    /// Successful password verifications for Basic-Auth/DataLinq requests are cached in memory for this period,
    /// because hashing (PBKDF2) is intentionally expensive. 0 = disabled.
    /// </summary>
    public int PasswordVerificationCacheSeconds { get; set; } = 300;
}
