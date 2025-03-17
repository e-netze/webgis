using E.Standard.Caching.Abstraction;
using E.Standard.Caching.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Caching.Services;

public class KeyValueCacheService : IKeyValueCache
{
    private const double DefaultAsideCacheExpirationSeconds = 300;  // 5 min;

    private readonly ISecurityConfigurationService _config;
    private readonly KeyValueCacheServiceOptions _options;

    private IKeyValueCache _keyValueCache = null;
    private IKeyValueCache _asideKeyValueCache = null;

    public KeyValueCacheService(ISecurityConfigurationService config,
                                IOptionsMonitor<KeyValueCacheServiceOptions> optionsMonitor)
    {
        _config = config;
        _options = optionsMonitor.CurrentValue;

        Init(String.Empty);
    }

    #region IKeyValueCache

    public string Get(string key)
    {
        var asideValue = _asideKeyValueCache?.Get(key);

        if (!String.IsNullOrEmpty(asideValue))
        {
            return asideValue;
        }

        var value = _keyValueCache.Get(key);

        if (_asideKeyValueCache != null && !String.IsNullOrEmpty(value))
        {
            _asideKeyValueCache.Set(key, value, DefaultAsideCacheExpirationSeconds);
        }

        return value;
    }

    public bool Init(string initalParameter)
    {
        if (_keyValueCache == null)
        {
            string cacheProvider = _config[_options.CacheProviderConfigValue];
            string cacheConnectionString = _config[_options.CacheConnectionStringConfigValue];

            _keyValueCache = Build(cacheProvider, cacheConnectionString, false, _config[_options.ImpersonateUserConfigValue]) ?? new DummyCache();

            string cacheAsideProvider = _config[_options.CacheAsideProviderConfigValue];
            string cacheAsideConnectionString = _config[_options.CacheAsideConnectionStringConfigValue];

            _asideKeyValueCache = Build(cacheAsideProvider, cacheAsideConnectionString, true, _config[_options.ImpersonateUserConfigValue]);
        }
        return true;
    }

    public void Remove(string key)
    {
        _keyValueCache.Remove(key);

        if (_asideKeyValueCache != null)
        {
            _asideKeyValueCache.Remove(key);
        }
    }

    public void Set(string key, object o, double expireSeconds = 0)
    {
        _keyValueCache.Set(key, o, expireSeconds.OrTake(_options.CacheExpireSecondsDefault)
                                                .OrTake(TimeSpan.FromDays(3650).TotalSeconds));

        if (_asideKeyValueCache != null)
        {
            _asideKeyValueCache.Set(key, o, expireSeconds.OrTake(_options.CacheAsideExpireSecondsDefault)
                                                         .OrTake(TimeSpan.FromDays(365).TotalMilliseconds));
        }
    }

    public void Set(string[] keys, string[] values, double expireSeconds = 0)
    {
        if (keys.Length != values.Length)
        {
            throw new ArgumentException("Lenght of key and value arrays not equal");
        }

        for (int i = 0; i < keys.Length; i++)
        {
            Set(keys[i], values[i], expireSeconds);
        }
    }

    public int MaxChunkSize
    {
        get
        {
            if (_keyValueCache == null)
            {
                return int.MaxValue;
            }

            if (_asideKeyValueCache != null)
            {
                return Math.Min(_keyValueCache.MaxChunkSize, _asideKeyValueCache.MaxChunkSize);
            }

            return _keyValueCache.MaxChunkSize;
        }
    }

    #endregion

    public Type KeyValueCacheType => _keyValueCache?.GetType();
    public Type KeyValueCacheAsideType => _asideKeyValueCache?.GetType();

    public IKeyValueCache KeyValueCacheInstance => _keyValueCache;
    public IKeyValueCache KeyValueCacheAsideInstance => _asideKeyValueCache;

    public string GetCacheValue(string key)
    {
        return _keyValueCache.Get(key);
    }

    public IEnumerable<string> GetCacheValues(IEnumerable<string> keys)
    {
        return keys.Select(key => GetCacheValue(key));
    }

    public IEnumerable<string> GetValues(IEnumerable<string> keys)
    {
        return keys.Select(key => Get(key));
    }

    public void UpdateCacheAside(string key)
    {
        if (_asideKeyValueCache == null)
        {
            return;
        }

        var value = _keyValueCache.Get(key);
        var asideValue = _asideKeyValueCache.Get(key);

        if (value != asideValue)
        {
            if (String.IsNullOrEmpty(value))
            {
                _asideKeyValueCache.Remove(key);
            }
            else
            {
                _asideKeyValueCache.Set(key, value, _options.CacheAsideExpireSecondsDefault.OrTake(DefaultAsideCacheExpirationSeconds));
            }
        }
    }

    public void UpdateCacheAside(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            UpdateCacheAside(key);
        }
    }

    #region Helper

    private IKeyValueCache Build(string cacheProvider, string connectionString, bool isAsideCache, string impersonateUser = "")
    {
        IKeyValueCache keyValueCache = null;

        if (!String.IsNullOrEmpty(connectionString) && cacheProvider.TryParseCacheProvider(out CacheProviderTypes providerType))
        {
            switch (providerType)
            {
                case CacheProviderTypes.Redis:
                    keyValueCache = new Redis.RedisCache(isAsideCache);
                    keyValueCache.Init(connectionString);
                    break;
                case CacheProviderTypes.Db:
                    keyValueCache = new Database.DbCache();
                    keyValueCache.Init(connectionString);
                    break;
                case CacheProviderTypes.Fs:
                    keyValueCache = new FileSystem.FileSystemCache(impersonateUser);
                    keyValueCache.Init(connectionString);
                    break;
                case CacheProviderTypes.Mongo:
                    keyValueCache = new Mongo.MongoCache();
                    keyValueCache.Init(connectionString);
                    break;
                case CacheProviderTypes.InApp:
                    keyValueCache = new InApp.InAppCache();
                    keyValueCache.Init(connectionString);
                    break;
            }
        }

        return keyValueCache;
    }

    #endregion
}
