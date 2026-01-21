namespace E.Standard.Caching.Abstraction;

public interface IKeyValueCache
{
    bool Init(string initalParameter);

    string Get(string key);
    void Set(string key, object o, double expireSeconds);

    string[] GetAllKeys();

    void Remove(string key);

    int MaxChunkSize { get; }
}

public interface IKeyValueCache<T>
{
    T Get(string key);
    void Set(string key, T data);

    void Remove(string key);
}
