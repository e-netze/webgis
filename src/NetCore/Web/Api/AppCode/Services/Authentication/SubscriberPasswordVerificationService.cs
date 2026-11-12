using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

using E.Standard.Api.App.Configuration;
using E.Standard.WebGIS.SubscriberDatabase;

using Microsoft.Extensions.Options;

namespace Api.Core.AppCode.Services.Authentication;

/// <summary>
/// Verifies subscriber passwords for per-request authentication (Basic-Auth, DataLinq code).
/// Password hashing (PBKDF2) is intentionally expensive, so successful verifications are cached
/// in memory for a short period. The cache key includes the stored hash, so a password change
/// invalidates cached entries immediately. Failed verifications are never cached.
/// </summary>
public class SubscriberPasswordVerificationService
{
    private const int MaxEntries = 10_000;

    private readonly ConcurrentDictionary<string, DateTime> _verified = new();
    private readonly SubscriberSecurityOptions _options;

    public SubscriberPasswordVerificationService(IOptions<SubscriberSecurityOptions> options)
    {
        _options = options.Value;
    }

    public bool VerifyPassword(SubscriberDb.Subscriber subscriber, string password)
    {
        if (subscriber == null || password == null)
        {
            return false;
        }

        if (_options.PasswordVerificationCacheSeconds <= 0)
        {
            return subscriber.VerifyPassword(password);
        }

        var key = CacheKey(subscriber, password);
        var now = DateTime.UtcNow;

        if (_verified.TryGetValue(key, out var expires))
        {
            if (expires > now)
            {
                return true;
            }
            _verified.TryRemove(key, out _);
        }

        if (!subscriber.VerifyPassword(password))
        {
            return false;
        }

        if (_verified.Count >= MaxEntries)
        {
            RemoveExpired(now);
            if (_verified.Count >= MaxEntries)
            {
                _verified.Clear();
            }
        }

        _verified[key] = now.AddSeconds(_options.PasswordVerificationCacheSeconds);

        return true;
    }

    private void RemoveExpired(DateTime now)
    {
        foreach (var entry in _verified)
        {
            if (entry.Value <= now)
            {
                _verified.TryRemove(entry.Key, out _);
            }
        }
    }

    private static string CacheKey(SubscriberDb.Subscriber subscriber, string password)
        => Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes($"{subscriber.Name?.ToLowerInvariant()}\0{password}\0{subscriber.PasswordHash}")));
}
