#nullable enable

using System;
using System.Collections.Concurrent;

namespace Api.Core.AppCode.Services.Authentication.Mfa;

/// <summary>
/// Short-lived cache of subscriber security stamps, so the auth cookie can be validated on every request
/// without a database roundtrip. Changes on this instance invalidate the entry immediately,
/// other instances (load balancing) pick them up after <see cref="Lifetime"/>.
/// </summary>
public class SubscriberSecurityStampCache
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);
    private const int MaxEntries = 10_000;

    private readonly ConcurrentDictionary<string, (string? stamp, DateTime expiresUtc)> _entries = new();

    public bool TryGet(string subscriberId, out string? stamp)
    {
        if (_entries.TryGetValue(subscriberId, out var entry) && entry.expiresUtc > DateTime.UtcNow)
        {
            stamp = entry.stamp;
            return true;
        }

        stamp = null;
        return false;
    }

    public void Set(string subscriberId, string? stamp)
    {
        if (_entries.Count >= MaxEntries)
        {
            _entries.Clear();
        }

        _entries[subscriberId] = (stamp, DateTime.UtcNow.Add(Lifetime));
    }

    public void Invalidate(string subscriberId) => _entries.TryRemove(subscriberId, out _);
}
