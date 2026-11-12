#nullable enable

using System;

using E.Standard.Api.App.Configuration;
using E.Standard.Security.Cryptography.Abstractions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Api.Core.AppCode.Services.Authentication;

public class ApiCookieAuthenticationService
{
    private readonly ICryptoService _crypto;
    private readonly SubscriberSecurityOptions _options;

    public ApiCookieAuthenticationService(ICryptoService crypto,
                                          IOptions<SubscriberSecurityOptions> options)
    {
        _crypto = crypto;
        _options = options.Value;
    }

    public void SetAuthCookie(HttpContext context, string cookieValue)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        AppendAuthCookie(context, $"{CookieFormatVersion}|{now}|{now}|{cookieValue}");
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

            // format: v2|created|lastActivity|value  (unix seconds)
            var parts = cookieValue.Split('|', 4);
            if (parts.Length != 4
                || parts[0] != CookieFormatVersion
                || !long.TryParse(parts[1], out long created)
                || !long.TryParse(parts[2], out long lastActivity))
            {
                // legacy cookies without timestamps are not accepted anymore
                SignOut(context);
                return null;
            }

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

                if (now - lastActivity > SlidingRefreshSeconds && !context.Response.HasStarted)
                {
                    AppendAuthCookie(context, $"{CookieFormatVersion}|{created}|{now}|{parts[3]}");
                }
            }

            return parts[3];
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
    private const string CookieFormatVersion = "v2";
    private const int SlidingRefreshSeconds = 60;
}
