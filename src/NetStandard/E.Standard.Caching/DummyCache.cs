using E.Standard.Caching.Abstraction;

namespace E.Standard.Caching;

public class DummyCache : IKeyValueCache
{
    #region ICache

    public bool Init(string initalParameter)
    {
        return true;
    }

    public T Get<T>(string key) where T : ICacheSerializable<T>
    {
        return default(T);
    }

    public string Get(string key)
    {
        return null;
    }

    public void Set(string key, object o, double expireSeconds)
    {

    }

    public bool Set(string[] keys, object[] objects)
    {
        return true;
    }

    public T Get<T>(long uid) where T : ICacheSerializable<T>
    {
        return default(T);
    }

    public string Get(long uid)
    {
        return null;
    }

    public void Set(long uid, object o)
    {

    }

    public void Remove(string key)
    {

    }

    public void Remove(long uid)
    {

    }

    public T[] Get<T>(string[] key, out bool hasNullValues) where T : ICacheSerializable<T>
    {
        hasNullValues = true;
        return null;
    }

    public T[] GetValues<T>(System.Collections.IEnumerable uids, out bool hasNullValues) where T : ICacheSerializable<T>
    {
        hasNullValues = true;
        return null;
    }

    public string[] GetAllKeys() => [];

    public int MaxChunkSize => int.MaxValue;

    #endregion
}
