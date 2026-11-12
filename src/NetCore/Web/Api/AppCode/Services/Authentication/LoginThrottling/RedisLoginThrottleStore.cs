using System;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Api.Core.AppCode.Services.Authentication.LoginThrottling;

/// <summary>
/// Shares login throttling state between multiple API instances.
/// If Redis is not reachable, the in-memory store is used as fallback (throttling then works per instance).
/// </summary>
public class RedisLoginThrottleStore : ILoginThrottleStore
{
    private const string KeyPrefix = "webgis:login-throttle:";

    private readonly Lazy<ConnectionMultiplexer> _connection;
    private readonly InMemoryLoginThrottleStore _fallback = new();
    private readonly ILogger _logger;

    public RedisLoginThrottleStore(string connectionString, ILogger logger)
    {
        _logger = logger;
        _connection = new Lazy<ConnectionMultiplexer>(() =>
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });
    }

    public LoginThrottleEntry Get(string key)
        => Execute(
            db =>
            {
                var value = db.StringGet(KeyPrefix + key);
                return value.HasValue ? JsonSerializer.Deserialize<LoginThrottleEntry>(value.ToString()) : null;
            },
            () => _fallback.Get(key));

    public void Set(string key, LoginThrottleEntry entry, TimeSpan timeToLive)
        => Execute(
            db => db.StringSet(KeyPrefix + key, JsonSerializer.Serialize(entry), timeToLive),
            () => { _fallback.Set(key, entry, timeToLive); return true; });

    public void Remove(string key)
        => Execute(
            db => db.KeyDelete(KeyPrefix + key),
            () => { _fallback.Remove(key); return true; });

    public bool TryAddMarker(string key, TimeSpan timeToLive)
        => Execute(
            db => db.StringSet(KeyPrefix + key, "1", timeToLive, When.NotExists),
            () => _fallback.TryAddMarker(key, timeToLive));

    private T Execute<T>(Func<IDatabase, T> redisAction, Func<T> fallbackAction)
    {
        try
        {
            return redisAction(_connection.Value.GetDatabase());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login throttle Redis store not available, using in-memory fallback");
            return fallbackAction();
        }
    }
}
