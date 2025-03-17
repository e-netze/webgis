using System;
using System.Collections.Concurrent;
using System.Linq;

namespace E.Standard.ThreadsafeClasses;

public class TemporaryCache<T>
{
    public TemporaryCache(int maxItemLifetimeSeconds)
    {
        this.MaxItemLifetimeSeconds = maxItemLifetimeSeconds;
    }

    #region Properties

    public int MaxItemLifetimeSeconds { get; set; }

    private ConcurrentDictionary<string, CacheItem> _cachedItems = new ConcurrentDictionary<string, CacheItem>();

    #endregion

    #region Methods

    public void Add(string key, T item, int maxLifetimeSeconds = 0)
    {

        LazyCollect();
        if (_cachedItems.ContainsKey(key))
        {
            _cachedItems[key] = new CacheItem(item, maxLifetimeSeconds);
        }
        else
        {
            _cachedItems.TryAdd(key, new CacheItem(item, maxLifetimeSeconds));
        }
    }

    public void Remove(string key)
    {
        if (_cachedItems.ContainsKey(key))
        {
            CacheItem cacheItem;
            _cachedItems.TryRemove(key, out cacheItem);
        }
    }

    public void RemoveResursive(string key)
    {
        foreach (var subKey in _cachedItems.Keys.Where(k => k.StartsWith(key + "/")))
        {
            Remove(subKey);
        }
        Remove(key);
    }

    public T this[string key]
    {
        get
        {
            LazyCollect();
            if (_cachedItems.ContainsKey(key))
            {
                try
                {
                    var cacheItem = _cachedItems[key];

                    if ((DateTime.UtcNow - cacheItem.Created).TotalSeconds > (cacheItem.MaxLifetimeSeconds > 0 ? cacheItem.MaxLifetimeSeconds : this.MaxItemLifetimeSeconds))
                    {
                        _cachedItems.TryRemove(key, out cacheItem);
                        return default(T);
                    }
                    return cacheItem.Value;
                }
                catch { }
            }
            return default(T);
        }
        set
        {
            Add(key, value);
        }
    }

    private DateTime _lastCollectTime = DateTime.UtcNow;
    public void Collect()
    {
        try
        {
            _lastCollectTime = DateTime.UtcNow;
            foreach (var key in _cachedItems.Keys.ToArray())
            {
                try
                {
                    var cacheItem = _cachedItems[key];
                    if ((DateTime.UtcNow - cacheItem.Created).TotalSeconds > (cacheItem.MaxLifetimeSeconds > 0 ? cacheItem.MaxLifetimeSeconds : this.MaxItemLifetimeSeconds))
                    {
                        _cachedItems.TryRemove(key, out cacheItem);
                    }
                }
                catch { }
            }
        }
        catch { }
    }

    private void LazyCollect()
    {
        if ((DateTime.UtcNow - _lastCollectTime).TotalSeconds > this.MaxItemLifetimeSeconds)
        {
            Collect();
        }
    }

    public void Clear()
    {
        _cachedItems.Clear();
    }

    #endregion

    #region Classes

    private class CacheItem
    {
        public CacheItem(T value, int maxLifetimeSeconds)
        {
            this.Value = value;
            this.Created = DateTime.UtcNow;
            this.MaxLifetimeSeconds = maxLifetimeSeconds;
        }

        public DateTime Created { get; private set; }
        public int MaxLifetimeSeconds { get; set; }

        public T Value { get; private set; }
    }

    #endregion
}
