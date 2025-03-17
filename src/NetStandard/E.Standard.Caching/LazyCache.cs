using System;
using System.Collections.Concurrent;

namespace E.Standard.Caching;

static public class LazyCache
{
    static private ConcurrentDictionary<string, LazyItem> _cache = new ConcurrentDictionary<string, LazyItem>();
    public const int MaxSeconds = 15;

    public delegate T GetCallback<T>();

    static public T Get<T>(string key, GetCallback<T> callback)
        where T : class
    {
        var lazyItem = _cache.ContainsKey(key) ? _cache[key] : null;

        if (lazyItem == null || lazyItem.IsExpired)
        {
            lazyItem = new LazyItem(callback());
            _cache[key] = lazyItem;
        }

        return lazyItem.Instance as T;
    }

    #region Classes

    private class LazyItem
    {
        private DateTime _creationDate = DateTime.UtcNow;

        public LazyItem(object instance)
        {
            this.Instance = instance;
        }

        public bool IsExpired
        {
            get
            {
                return (DateTime.UtcNow - _creationDate).TotalSeconds > MaxSeconds;
            }
        }

        public object Instance { get; private set; }
    }

    #endregion
}
