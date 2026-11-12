using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Api.Core.AppCode.Services.Authentication.LoginThrottling;

public class InMemoryLoginThrottleStore : ILoginThrottleStore
{
    private const int CleanupInterval = 500;

    private readonly ConcurrentDictionary<string, Item> _items = new();
    private int _operations;

    public LoginThrottleEntry Get(string key)
    {
        if (_items.TryGetValue(key, out var item))
        {
            if (item.ExpiresUtc > DateTime.UtcNow)
            {
                return item.Entry is null ? null : Copy(item.Entry);
            }
            _items.TryRemove(key, out _);
        }
        return null;
    }

    public void Set(string key, LoginThrottleEntry entry, TimeSpan timeToLive)
    {
        _items[key] = new Item(Copy(entry), DateTime.UtcNow.Add(timeToLive));
        CleanupIfNeeded();
    }

    public void Remove(string key) => _items.TryRemove(key, out _);

    public bool TryAddMarker(string key, TimeSpan timeToLive)
    {
        var now = DateTime.UtcNow;
        var added = new Item(null, now.Add(timeToLive));

        while (true)
        {
            if (_items.TryAdd(key, added))
            {
                CleanupIfNeeded();
                return true;
            }
            if (_items.TryGetValue(key, out var existing))
            {
                if (existing.ExpiresUtc > now)
                {
                    return false;
                }
                if (_items.TryUpdate(key, added, existing))
                {
                    return true;
                }
            }
        }
    }

    private void CleanupIfNeeded()
    {
        if (Interlocked.Increment(ref _operations) % CleanupInterval != 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var item in _items)
        {
            if (item.Value.ExpiresUtc <= now)
            {
                _items.TryRemove(item.Key, out _);
            }
        }
    }

    private static LoginThrottleEntry Copy(LoginThrottleEntry entry) => new()
    {
        FailedAttempts = entry.FailedAttempts,
        FirstFailureUtc = entry.FirstFailureUtc,
        LastFailureUtc = entry.LastFailureUtc,
        LockedUntilUtc = entry.LockedUntilUtc
    };

    private record Item(LoginThrottleEntry Entry, DateTime ExpiresUtc);
}
