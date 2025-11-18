using E.Standard.Caching.Abstraction;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace E.Standard.Caching.Redis;

public class RedisCache : IKeyValueCache
{
    private static ConnectionMultiplexer _connection = null;
    private static long _cacheExpireSeconds = 3600; //60;

    public RedisCache(bool isAsideCache)
    {
        this.IsAsideCache = isAsideCache;
    }

    public static int CacheObjectExpireSeconds
    {
        set
        {
            _cacheExpireSeconds = value;
        }
    }
    public static TimeSpan? CacheObjectExpires
    {
        get
        {
            if (_cacheExpireSeconds <= 0)
            {
                return null;
            }

            return TimeSpan.FromSeconds(_cacheExpireSeconds);
        }
    }

    #region ICache

    public bool IsAsideCache { get; }

    public bool Init(string initalParameter)
    {
        try
        {
            _connection = ConnectionMultiplexer.Connect(initalParameter);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public T Get<T>(string key) where T : ICacheSerializable<T>
    {
        try
        {
            if (_connection != null)
            {
                IDatabase cache = _connection.GetDatabase();
                return cache.Get<T>(key);
            }
        }
        catch (Exception)
        {
            if (!IsAsideCache)
            {
                throw;
            }
        }

        return default(T);
    }

    public T[] Get<T>(string[] keys, out bool hasNullValues) where T : ICacheSerializable<T>
    {
        try
        {
            if (_connection != null)
            {
                IDatabase cache = _connection.GetDatabase();
                return cache.Get<T>(keys, out hasNullValues);
            }
        }
        catch (Exception)
        {
            if (!IsAsideCache)
            {
                throw;
            }
        }

        hasNullValues = false;
        return new T[0];
    }


    public string Get(string key)
    {
        try
        {
            if (_connection != null)
            {
                IDatabase cache = _connection.GetDatabase();
                return cache.StringGet(key);
            }
        }
        catch (Exception)
        {
            if (!IsAsideCache)
            {
                throw;
            }
        }

        return null;
    }

    public void Set(string key, object o, double expireSeconds)
    {
        try
        {
            if (_connection != null)
            {
                IDatabase cache = _connection.GetDatabase();
                if (o == null)
                {
                    Remove(key);
                }
                else
                {
                    cache.Set(key, o);
                }
            }
        }
        catch (Exception)
        {
            if (!IsAsideCache)
            {
                throw;
            }
        }
    }

    public bool Set(string[] keys, object[] values)
    {
        try
        {
            if (_connection != null)
            {
                IDatabase cache = _connection.GetDatabase();
                return cache.Set(keys, values);
            }
        }
        catch (Exception)
        {
            if (!IsAsideCache)
            {
                throw;
            }
        }

        return false;
    }

    public T Get<T>(long uid) where T : ICacheSerializable<T>
    {
        return Get<T>(uid.ToString());
    }

    public T[] GetValues<T>(System.Collections.IEnumerable keyValues, out bool hasNullValues) where T : ICacheSerializable<T>
    {
        List<string> keys = new List<string>();

        foreach (object keyValue in keyValues)
        {
            if (keyValue != null)
            {
                keys.Add(keyValue.ToString());
            }
        }

        return Get<T>(keys.ToArray(), out hasNullValues);
    }

    public string Get(long uid)
    {
        return Get(uid.ToString());
    }

    public void Set(long uid, object o, double expireSeconds)
    {
        if (o == null)
        {
            Remove(uid);
        }
        else
        {
            Set(uid.ToString(), o, expireSeconds);
        }
    }

    public void Remove(string key)
    {
        try
        {
            if (_connection != null)
            {
                IDatabase cache = _connection.GetDatabase();
                cache.KeyDelete(key, CommandFlags.FireAndForget);
            }
        }
        catch (Exception)
        {
            if (!IsAsideCache)
            {
                throw;
            }
        }
    }

    public void Remove(long uid)
    {
        Remove(uid.ToString());
    }

    public string[] GetAllKeys()
    {
        try
        {
            List<string> keys = new List<string>();

            if (_connection != null)
            {
                foreach (var endpoint in _connection.GetEndPoints())
                {
                    var server = _connection.GetServer(endpoint);

                    // ensure, it realy a server (not a sentinel)
                    if (server == null || !server.IsConnected)
                        continue;

                    foreach (var key in server.Keys(pattern: "*"))
                    {
                        keys.Add(key);
                    }
                }
            }

            return keys.ToArray();
        }
        catch (Exception)
        {
            if (!IsAsideCache)
            {
                throw;
            }
        }

        return [];
    }

    public int MaxChunkSize => int.MaxValue;

    #endregion        
}
