using E.Standard.Caching.Abstraction;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace E.Standard.Caching.InApp;

public class InAppCache : IKeyValueCache, ICacheClearable
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();
    private int _expireSeconds = 3600;

    public int MaxChunkSize => int.MaxValue;

    public Task<bool> Clear()
    {
        _cache.Clear();

        return Task.FromResult(true);
    }

    public string Get(string key)
    {
        if (_cache.TryGetValue(key, out CacheItem item))
        {
            if (item != null)
            {
                if (item.IsExpired)
                {
                    this.Remove(key);
                }
                else
                {
                    return item.Value;
                }
            }
        }

        return null;
    }

    public bool Init(string initalParameter)
    {
        if (int.TryParse(initalParameter, out int seconds))
        {
            _expireSeconds = seconds;
        }

        return true;
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out CacheItem item);
    }

    public void Set(string key, object o, double expireSeconds)
    {
        _cache[key] = new CacheItem(o?.ToString(), _expireSeconds);
    }

    #region Classes

    private class CacheItem
    {
        private readonly long _expiresTicks;

        public CacheItem(string value, int expireSeconds)
        {
            _expiresTicks = DateTime.UtcNow.AddSeconds(expireSeconds).Ticks;

            this.Value = value;
        }

        public string Value
        {
            get; private set;
        }

        public bool IsExpired => DateTime.UtcNow.Ticks >= _expiresTicks;
    }

    #endregion
}
