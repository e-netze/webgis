#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Api.Core.AppCode.Services.Security;

using static SecurityVerdict;

/// <summary>
/// Central definition of all security-relevant api.config keys of the security report.
/// Keep in sync with the table "Overview of all security-relevant keys" in the Security annex
/// of the admin documentation (annex/security.rst): categories, risks and recommendations
/// must match.
/// </summary>
public static class SecurityReportRules
{
    private const string Login = "annex-security-subscriber-login";
    private const string Password = "annex-security-password";
    private const string Mfa = "annex-security-mfa";
    private const string CaptchaLockout = "annex-security-captcha-lockout";
    private const string Sessions = "annex-security-sessions";
    private const string Redirects = "annex-security-redirects";
    private const string DynamicContent = "annex-security-dynamic-content";
    private const string Tls = "annex-security-tls";
    private const string Csrf = "annex-security-csrf";
    private const string Endpoints = "annex-security-endpoints";
    private const string Proxy = "annex-security-proxy";
    private const string Keys = "annex-security-keys";

    private const string DefaultTermWhiteList = @"^[\p{L}\p{N} _.,%/#:()+&@'\-]{0,120}$";

    private const SecurityCategory CompatOptOut = SecurityCategory.CompatibilityOptOut;
    private const SecurityCategory Feature = SecurityCategory.FeatureEnablement;
    private const SecurityCategory Fine = SecurityCategory.FineTuning;

    public static IReadOnlyList<SecurityRule> Rules { get; } = Build();

    private static SecurityVerdict V(bool ok) => ok ? Ok : Deviation;

    private static SecurityVerdict InRange(SecurityRuleContext c, string key, long def, long min, long max)
    {
        var v = c.Int(key, def);
        return V(v >= min && v <= max);
    }

    // secure endpoint credentials: internet => deviation, intranet => hint when missing
    private static SecurityVerdict EndpointCredentials(SecurityRuleContext c, bool internet, string key)
    {
        var urlPassword = c.IsSet("security:secure-endpoint-url-password");
        var user = c.IsSet("security:secure-endpoint-basicauth-username");
        var password = c.IsSet("security:secure-endpoint-basicauth-password");

        // username without password (or vice versa) is an incomplete configuration
        if (user != password && key != "security:secure-endpoint-url-password")
        {
            return Deviation;
        }

        if (urlPassword || (user && password))
        {
            return Ok;
        }

        return internet ? Deviation : Hint;
    }

    private static SecurityRule Rule(string key, string def, SecurityCategory cat, SecurityRisk risk, string anchor,
                                     Func<SecurityRuleContext, SecurityVerdict> both,
                                     bool secret = false, bool requiresLogin = false)
        => new(key, def, cat, risk, anchor, both, both, secret, requiresLogin);

    private static SecurityRule Rule(string key, string def, SecurityCategory cat, SecurityRisk risk, string anchor,
                                     Func<SecurityRuleContext, SecurityVerdict> internet,
                                     Func<SecurityRuleContext, SecurityVerdict> intranet,
                                     bool secret = false, bool requiresLogin = false)
        => new(key, def, cat, risk, anchor, internet, intranet, secret, requiresLogin);

    private static IReadOnlyList<SecurityRule> Build() => new List<SecurityRule>
    {
        Rule("allow-subscriber-login", "false", Feature, SecurityRisk.High, Login,
            c => V(!c.Bool("allow-subscriber-login", false)),
            c => Ok),
        Rule("allow-register-new-subscribers", "false", Feature, SecurityRisk.High, Login,
            c => V(!c.Bool("allow-register-new-subscribers", false))),
        Rule("subscriber-db-connectionstring", "-", Fine, SecurityRisk.Info, Login,
            c => c.IsSet("subscriber-db-connectionstring") ? Ok : NotApplicable, secret: true),
        Rule("subscriber-login-allowed-redirect-hosts", "-", Fine, SecurityRisk.Medium, Redirects,
            c => c.IsSet("subscriber-login-allowed-redirect-hosts") ? Hint : Ok, requiresLogin: true),

        Rule("security:cookie-secure", "auto", Fine, SecurityRisk.Medium, Sessions,
            c => V(c.Is("security:cookie-secure", "auto", "auto", "always")), requiresLogin: true),
        Rule("security:session-max-lifetime-minutes", "480", Fine, SecurityRisk.Medium, Sessions,
            c => InRange(c, "security:session-max-lifetime-minutes", 480, 1, 480), requiresLogin: true),
        Rule("security:session-idle-timeout-minutes", "0", Fine, SecurityRisk.Low, Sessions,
            c => InRange(c, "security:session-idle-timeout-minutes", 0, 1, 60),
            c => Ok, requiresLogin: true),

        Rule("security:password-min-length", "12", Fine, SecurityRisk.Medium, Password,
            c => V(c.Int("security:password-min-length", 12) >= 12), requiresLogin: true),
        Rule("security:password-required-character-classes", "0", Fine, SecurityRisk.Low, Password,
            c => Ok, requiresLogin: true),
        Rule("security:password-blocklist-file", "-", Fine, SecurityRisk.Low, Password,
            c => V(c.IsSet("security:password-blocklist-file")),
            c => Ok, requiresLogin: true),

        Rule("security:login-captcha-after-failed-attempts", "3", Fine, SecurityRisk.Medium, CaptchaLockout,
            c => InRange(c, "security:login-captcha-after-failed-attempts", 3, 1, 3), requiresLogin: true),
        Rule("security:login-lockout-after-failed-attempts", "10", Fine, SecurityRisk.Medium, CaptchaLockout,
            c => InRange(c, "security:login-lockout-after-failed-attempts", 10, 1, 10), requiresLogin: true),
        Rule("security:login-lockout-minutes", "1,5,15", Fine, SecurityRisk.Low, CaptchaLockout,
            c => Ok, requiresLogin: true),
        Rule("security:login-failed-attempts-reset-minutes", "60", Fine, SecurityRisk.Low, CaptchaLockout,
            c => Ok, requiresLogin: true),
        Rule("security:login-ip-max-failed-attempts", "30", Fine, SecurityRisk.Low, CaptchaLockout,
            c => InRange(c, "security:login-ip-max-failed-attempts", 30, 1, 30), requiresLogin: true),
        Rule("security:login-ip-window-minutes", "15", Fine, SecurityRisk.Low, CaptchaLockout,
            c => Ok, requiresLogin: true),
        Rule("security:login-throttle-redis-connectionstring", "-", Fine, SecurityRisk.Low, CaptchaLockout,
            c => c.IsSet("security:login-throttle-redis-connectionstring") ? Ok : Hint,
            c => Ok, secret: true, requiresLogin: true),
        Rule("security:captcha-lifetime-minutes", "5", Fine, SecurityRisk.Low, CaptchaLockout,
            c => Ok, requiresLogin: true),
        Rule("security:password-verification-cache-seconds", "300", Fine, SecurityRisk.Low, Password,
            c => InRange(c, "security:password-verification-cache-seconds", 300, 0, 300)),

        Rule("security:mfa-required", "none", Fine, SecurityRisk.Medium, Mfa,
            c => V(c.Is("security:mfa-required", "none", "admins", "all")),
            c => c.Is("security:mfa-required", "none", "admins", "all") ? Ok : Hint,
            requiresLogin: true),
        Rule("security:mfa-issuer", "WebGIS", Fine, SecurityRisk.Info, Mfa,
            c => Ok, requiresLogin: true),

        Rule("security:allow-untrusted-server-certificates", "false", CompatOptOut, SecurityRisk.High, Tls,
            c => V(!c.Bool("security:allow-untrusted-server-certificates", false))),
        Rule("security:disable-anti-forgery", "false", CompatOptOut, SecurityRisk.High, Csrf,
            c => V(!c.Bool("security:disable-anti-forgery", false))),

        Rule("security:secure-endpoint-url-password", "-", Fine, SecurityRisk.Medium, Endpoints,
            c => EndpointCredentials(c, true, "security:secure-endpoint-url-password"),
            c => EndpointCredentials(c, false, "security:secure-endpoint-url-password"), secret: true),
        Rule("security:secure-endpoint-basicauth-username", "-", Fine, SecurityRisk.Medium, Endpoints,
            c => EndpointCredentials(c, true, "security:secure-endpoint-basicauth-username"),
            c => EndpointCredentials(c, false, "security:secure-endpoint-basicauth-username")),
        Rule("security:secure-endpoint-basicauth-password", "-", Fine, SecurityRisk.Medium, Endpoints,
            c => EndpointCredentials(c, true, "security:secure-endpoint-basicauth-password"),
            c => EndpointCredentials(c, false, "security:secure-endpoint-basicauth-password"), secret: true),

        Rule("dynamic-content:allow-hosts", "-", CompatOptOut, SecurityRisk.High, DynamicContent,
            c => V(!c.IsSet("dynamic-content:allow-hosts")),
            c => V(!c.Str("dynamic-content:allow-hosts", "").Contains('*'))),
        Rule("dynamic-content:allow-unsigned", "false", CompatOptOut, SecurityRisk.High, DynamicContent,
            c => V(!c.Bool("dynamic-content:allow-unsigned", false))),
        Rule("dynamic-content:timeout-seconds", "20", Fine, SecurityRisk.Low, DynamicContent,
            c => InRange(c, "dynamic-content:timeout-seconds", 20, 1, 20)),
        Rule("dynamic-content:max-response-bytes", "52428800", Fine, SecurityRisk.Low, DynamicContent,
            c => InRange(c, "dynamic-content:max-response-bytes", 52428800, 1, 52428800)),
        Rule("dynamic-content:max-redirects", "5", Fine, SecurityRisk.Low, DynamicContent,
            c => InRange(c, "dynamic-content:max-redirects", 5, 0, 5)),

        Rule("lookup:term-whitelist", "(default)", Fine, SecurityRisk.Medium, Keys,
            c => !c.IsSet("lookup:term-whitelist") || c.Str("lookup:term-whitelist", "") == DefaultTermWhiteList ? Ok : Hint),

        Rule("middleware:use-x-forwarded-headers", "false", Fine, SecurityRisk.Medium, Proxy,
            c => c.Bool("middleware:use-x-forwarded-headers", false) ? Hint : Ok),
        Rule("middleware:x-forwarded-known-proxies", "-", Fine, SecurityRisk.Medium, Proxy,
            c => c.Bool("middleware:use-x-forwarded-headers", false) && !c.IsSet("middleware:x-forwarded-known-proxies") && !c.IsSet("middleware:x-forwarded-known-networks")
                 ? Deviation : NotApplicable),
        Rule("middleware:x-forwarded-known-networks", "-", Fine, SecurityRisk.Medium, Proxy,
            c => c.Bool("middleware:use-x-forwarded-headers", false) && !c.IsSet("middleware:x-forwarded-known-proxies") && !c.IsSet("middleware:x-forwarded-known-networks")
                 ? Deviation : NotApplicable),
        Rule("middleware:use-x-forwarded-headers-logging", "false", Fine, SecurityRisk.Info, Proxy,
            c => c.Bool("middleware:use-x-forwarded-headers-logging", false) ? Hint : Ok),
    };
}
