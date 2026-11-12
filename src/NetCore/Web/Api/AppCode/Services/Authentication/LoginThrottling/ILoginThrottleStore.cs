using System;

namespace Api.Core.AppCode.Services.Authentication.LoginThrottling;

/// <summary>
/// Storage for login throttling state. Implementations: in-memory (per instance, default) or Redis (shared between instances).
/// Updates are not atomic across instances; counters are approximate, which is sufficient for throttling.
/// </summary>
public interface ILoginThrottleStore
{
    LoginThrottleEntry Get(string key);

    void Set(string key, LoginThrottleEntry entry, TimeSpan timeToLive);

    void Remove(string key);

    /// <summary>
    /// Adds a marker key if it does not exist yet. Returns false if the key already exists (e.g. a captcha nonce was already used).
    /// </summary>
    bool TryAddMarker(string key, TimeSpan timeToLive);
}

public class LoginThrottleEntry
{
    public int FailedAttempts { get; set; }
    public DateTime FirstFailureUtc { get; set; }
    public DateTime LastFailureUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}
