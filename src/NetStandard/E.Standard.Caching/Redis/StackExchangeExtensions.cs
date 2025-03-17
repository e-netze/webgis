using E.Standard.Caching.Abstraction;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace E.Standard.Caching.Redis;

public static class StackExchangeRedisExtensions
{
    public static T Get<T>(this IDatabase cache, string key) where T : ICacheSerializable<T>
    {
        T ret = Deserialize<T>(cache.StringGet(key));
        /*
        TimeSpan? expires = RedisCache.CacheObjectExpires;
        if (expires != null)
            cache.KeyExpire(key, expires, CommandFlags.FireAndForget);
        */
        return ret;
    }

    public static T[] Get<T>(this IDatabase cache, string[] keys, out bool hasNullValues) where T : ICacheSerializable<T>
    {
        hasNullValues = false;

        if (keys == null)
        {
            return new T[0];
        }

        int len = keys.Length;
        if (len == 0)
        {
            return new T[0];
        }

        RedisKey[] redisKeys = new RedisKey[keys.Length];
        for (int i = 0; i < len; i++)
        {
            redisKeys[i] = keys[i];
        }

        List<T> ret = new List<T>();
        RedisValue[] redisValues = cache.StringGet(redisKeys);
        for (int i = 0, to = redisValues.Length; i < to; i++)
        {
            T t = Deserialize<T>(redisValues[i]);
            ret.Add(t);
            if (t == null)
            {
                hasNullValues = true;
            }
        }

        return ret.ToArray();
    }

    public static void Set(this IDatabase cache, string key, object value)
    {
        if (value is string)
        {
            cache.StringSet(key, (string)value, RedisCache.CacheObjectExpires, When.Always, CommandFlags.FireAndForget);
        }
        else if (value is ICacheSerializable)
        {
            cache.StringSet(key, ((ICacheSerializable)value).Serialize(), ExpireTimeSpan((ICacheSerializable)value), When.Always, CommandFlags.FireAndForget);
        }
    }

    public static bool Set(this IDatabase cache, string[] keys, object[] values)
    {
        if (keys == null || values == null || keys.Length != values.Length)
        {
            return false;
        }

        bool success = false;
        var trans = cache.CreateTransaction();

        KeyValuePair<RedisKey, RedisValue>[] kvp = new KeyValuePair<RedisKey, RedisValue>[keys.Length];
        Dictionary<RedisKey, TimeSpan?> keysExpires = new Dictionary<RedisKey, TimeSpan?>();

        for (int i = 0, to = keys.Length; i < to; i++)
        {
            RedisKey redisKey = new RedisKey();
            redisKey = keys[i];

            RedisValue redisVal = new RedisValue();
            if (values[i] is string)
            {
                redisVal = (string)values[i];
                keysExpires.Add(redisKey, RedisCache.CacheObjectExpires);
            }
            else if (values[i] is ICacheSerializable)
            {
                redisVal = ((ICacheSerializable)values[i]).Serialize();
                keysExpires.Add(redisKey, ExpireTimeSpan((ICacheSerializable)values[i]));
            }

            kvp[i] = new KeyValuePair<RedisKey, RedisValue>(redisKey, redisVal);
        }

        trans.StringSetAsync(kvp, When.Always, CommandFlags.FireAndForget);
        foreach (var redisKey in keysExpires.Keys)
        {
            TimeSpan? expires = keysExpires[redisKey];
            if (expires == null)
            {
                continue;
            }

            trans.KeyExpireAsync(redisKey, expires);
        }

        success = trans.Execute();

        return success;
    }

    static T Deserialize<T>(byte[] stream) where T : ICacheSerializable<T>
    {
        if (stream == null)
        {
            return default(T);
        }

        try
        {
            var instance = Activator.CreateInstance(typeof(T)) as ICacheSerializable<T>;
            instance = instance.Deserialize(stream);
            return (T)instance;
        }
        catch
        {
            return default(T);
        }
    }

    static private TimeSpan? ExpireTimeSpan(ICacheSerializable serializable)
    {
        if (serializable.Expires < 0)
        {
            return null;
        }

        if (serializable.Expires > 0)
        {
            return TimeSpan.FromSeconds(serializable.Expires);
        }

        return RedisCache.CacheObjectExpires;
    }
}
