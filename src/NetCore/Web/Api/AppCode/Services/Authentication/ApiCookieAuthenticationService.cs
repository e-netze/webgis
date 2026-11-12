#nullable enable

using System;

using Api.Core.AppCode.Services.Authentication.Mfa;

using E.Standard.Api.App.Configuration;
using E.Standard.Security.Cryptography.Abstractions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Api.Core.AppCode.Services.Authentication;

public class ApiCookieAuthenticationService
{
    private readonly ICryptoService _crypto;
    private readonly SubscriberAccountSecurityService _accountSecurity;
    private readonly SubscriberSecurityOptions _options;

    public ApiCookieAuthenticationService(ICryptoService crypto,
                                          SubscriberAccountSecurityService accountSecurity,
                                          IOptions<SubscriberSecurityOptions> options)
    {
        _crypto = crypto;
        _accountSecurity = accountSecurity;
        _options = options.Value;
    }

    /// <param name="subscriberId">
    /// If set, the cookie is bound to the current security stamp of this subscriber
    /// => it gets invalid when the password or the MFA settings change.
    /// </param>
    public void SetAuthCookie(HttpContext context, string cookieValue, string? subscriberId = null)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string stamp = String.IsNullOrEmpty(subscriberId)
            ? String.Empty
            : _accountSecurity.GetSecurityStamp(subscriberId) ?? String.Empty;

        AppendAuthCookie(context, Payload(now, now, subscriberId, stamp, cookieValue));
        context.Response.Headers.Append("P3P", "CP='IDC DSP COR ADM DEVi TAIi PSA PSD IVAi IVDi CONi HIS OUR IND CNT'");
    }

    public void SignOut(HttpContext context)
    {
        context.Response.Cookies.Delete(AuthCookieName, CreateCookieOptions(context));
    }

    public string? TryGetCookieUsername(HttpContext context)
    {
        try
        {
            string? cookie = context.Request.Cookies[AuthCookieName];
            if (String.IsNullOrEmpty(cookie))
            {
                return null;
            }

            string cookieValue = _crypto.DecryptCookieValue(cookie) ?? String.Empty;

            // format: v3|created|lastActivity|subscriberId|securityStamp|value  (unix seconds)
            var parts = cookieValue.Split('|', 6);
            if (parts.Length != 6
                || parts[0] != CookieFormatVersion
                || !long.TryParse(parts[1], out long created)
                || !long.TryParse(parts[2], out long lastActivity))
            {
                // legacy cookies (without timestamps or security stamp) are not accepted anymore
                SignOut(context);
                return null;
            }

            string subscriberId = parts[3], stamp = parts[4], value = parts[5];
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (_options.SessionMaxLifetimeMinutes > 0
                && now - created > _options.SessionMaxLifetimeMinutes * 60L)
            {
                SignOut(context);
                return null;
            }

            if (_options.SessionIdleTimeoutMinutes > 0)
            {
                if (now - lastActivity > _options.SessionIdleTimeoutMinutes * 60L)
                {
                    SignOut(context);
                    return null;
                }
            }

            if (!String.IsNullOrEmpty(subscriberId))
            {
                // null => can't be checked (MFA storage not available)
                string? currentStamp = _accountSecurity.GetSecurityStamp(subscriberId);
                if (currentStamp != null && currentStamp != stamp)
                {
                    // password or MFA settings changed => session is invalid
                    SignOut(context);
                    return null;
                }
            }

            if (_options.SessionIdleTimeoutMinutes > 0
                && now - lastActivity > SlidingRefreshSeconds
                && !context.Response.HasStarted)
            {
                AppendAuthCookie(context, Payload(created, now, subscriberId, stamp, value));
            }

            return value;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            SignOut(context);
        }
        catch (E.Standard.Security.Cryptography.Exceptions.CryptographyException)
        {
            SignOut(context);
        }
        catch { }

        return null;
    }

    private void AppendAuthCookie(HttpContext context, string payload)
    {
        context.Response.Cookies.Append(AuthCookieName, _crypto.EncryptCookieValue(payload), CreateCookieOptions(context));
    }

    private static string Payload(long created, long lastActivity, string? subscriberId, string stamp, string value)
        => $"{CookieFormatVersion}|{created}|{lastActivity}|{subscriberId?.Replace("|", "")}|{stamp}|{value}";

    private CookieOptions CreateCookieOptions(HttpContext context)
        => new CookieOptions()
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Secure = _options.CookieSecure switch
            {
                CookieSecureMode.Always => true,
                CookieSecureMode.Never => false,
                _ => context.Request.IsHttps
            }
        };

    private const string AuthCookieName = "webgis5core-api-auth";
    private const string CookieFormatVersion = "v3";
    private const int SlidingRefreshSeconds = 60;
}
