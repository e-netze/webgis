namespace E.Standard.Caching;

public enum CacheProviderTypes
{
    Unknown,
    Db,        // Database
    Fs,        // FileSystem
    Redis,
    Mongo,
    InApp
}
