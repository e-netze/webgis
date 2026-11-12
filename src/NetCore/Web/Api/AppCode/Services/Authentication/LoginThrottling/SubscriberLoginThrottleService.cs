using System;
using System.Linq;

using E.Standard.Api.App.Configuration;

using Microsoft.Extensions.Options;

namespace Api.Core.AppCode.Services.Authentication.LoginThrottling;

public record LoginThrottleStatus(bool CaptchaRequired, bool IpThrottled, int LockedSeconds)
{
    public bool IsLocked => LockedSeconds > 0;
}

/// <summary>
/// Protects the subscriber forms login against brute force/credential stuffing:
/// <list type="bullet">
/// <item>per username: captcha after n failures, escalating temporary lockout after m failures</item>
/// <item>per client IP: after n failures within a window every login requires a captcha and is slowed down (never a hard block,
/// because many users can share one IP behind a proxy/NAT)</item>
/// </list>
/// </summary>
public class SubscriberLoginThrottleService
{
    private readonly ILoginThrottleStore _store;
    private readonly SubscriberSecurityOptions _options;

    public SubscriberLoginThrottleService(ILoginThrottleStore store, IOptions<SubscriberSecurityOptions> options)
    {
        _store = store;
        _options = options.Value;
    }

    public LoginThrottleStatus GetStatus(string username, string clientIp)
    {
        var now = DateTime.UtcNow;
        var userEntry = GetUserEntry(username, now);
        var ipThrottled = IsIpThrottled(clientIp, now);

        var lockedSeconds = userEntry?.LockedUntilUtc > now
            ? (int)Math.Ceiling((userEntry.LockedUntilUtc.Value - now).TotalSeconds)
            : 0;

        var captchaRequired =
            _options.LoginCaptchaAfterFailedAttempts == 0 ||
            (userEntry?.FailedAttempts ?? 0) >= _options.LoginCaptchaAfterFailedAttempts ||
            ipThrottled;

        return new LoginThrottleStatus(captchaRequired, ipThrottled, lockedSeconds);
    }

    /// <summary>
    /// Registers a failed login. A wrong captcha should only be counted for the client IP (countForUsername = false),
    /// otherwise anybody could lock out a user without solving captchas.
    /// </summary>
    public void RegisterFailure(string username, string clientIp, bool countForUsername = true)
    {
        var now = DateTime.UtcNow;

        if (countForUsername && !String.IsNullOrWhiteSpace(username))
        {
            var entry = GetUserEntry(username, now) ?? new LoginThrottleEntry() { FirstFailureUtc = now };
            entry.FailedAttempts++;
            entry.LastFailureUtc = now;

            if (_options.LoginLockoutAfterFailedAttempts > 0 &&
                entry.FailedAttempts >= _options.LoginLockoutAfterFailedAttempts &&
                _options.LoginLockoutMinutes?.Length > 0)
            {
                var step = Math.Min(entry.FailedAttempts - _options.LoginLockoutAfterFailedAttempts, _options.LoginLockoutMinutes.Length - 1);
                entry.LockedUntilUtc = now.AddMinutes(_options.LoginLockoutMinutes[step]);
            }

            var timeToLive = TimeSpan.FromMinutes(Math.Max(_options.LoginFailedAttemptsResetMinutes,
                                                            _options.LoginLockoutMinutes?.DefaultIfEmpty(0).Max() ?? 0) + 1);
            _store.Set(UserKey(username), entry, timeToLive);
        }

        if (_options.LoginIpMaxFailedAttempts > 0 && !String.IsNullOrWhiteSpace(clientIp))
        {
            var entry = GetIpEntry(clientIp, now) ?? new LoginThrottleEntry() { FirstFailureUtc = now };
            entry.FailedAttempts++;
            entry.LastFailureUtc = now;

            var timeToLive = entry.FirstFailureUtc.AddMinutes(_options.LoginIpWindowMinutes) - now;
            _store.Set(IpKey(clientIp), entry, timeToLive > TimeSpan.Zero ? timeToLive : TimeSpan.FromSeconds(1));
        }
    }

    public void RegisterSuccess(string username)
    {
        if (!String.IsNullOrWhiteSpace(username))
        {
            _store.Remove(UserKey(username));
        }
    }

    /// <summary>
    /// Marks a captcha (nonce) as used. Returns false if it was already used before (replay).
    /// </summary>
    public bool TryConsumeCaptchaNonce(string nonce)
        => !String.IsNullOrEmpty(nonce) &&
           _store.TryAddMarker($"captcha:{nonce}", TimeSpan.FromMinutes(Math.Max(1, _options.CaptchaLifetimeMinutes) + 1));

    #region Helper

    private LoginThrottleEntry GetUserEntry(string username, DateTime now)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var entry = _store.Get(UserKey(username));
        if (entry != null &&
            entry.LastFailureUtc.AddMinutes(_options.LoginFailedAttemptsResetMinutes) < now &&
            !(entry.LockedUntilUtc > now))
        {
            return null;
        }
        return entry;
    }

    private LoginThrottleEntry GetIpEntry(string clientIp, DateTime now)
    {
        var entry = _store.Get(IpKey(clientIp));
        if (entry != null && entry.FirstFailureUtc.AddMinutes(_options.LoginIpWindowMinutes) < now)
        {
            return null;
        }
        return entry;
    }

    private bool IsIpThrottled(string clientIp, DateTime now)
        => _options.LoginIpMaxFailedAttempts > 0 &&
           !String.IsNullOrWhiteSpace(clientIp) &&
           (GetIpEntry(clientIp, now)?.FailedAttempts ?? 0) >= _options.LoginIpMaxFailedAttempts;

    private static string UserKey(string username) => $"user:{username.Trim().ToLowerInvariant()}";
    private static string IpKey(string clientIp) => $"ip:{clientIp}";

    #endregion
}
