using System;

namespace E.Standard.Caching.Extensions;

public static class StringExtensions
{
    #region Mongo ConnectionString

    public static string GetMongoConnetion(this string path)
    {
        return path.GetPathValue("connection");
    }

    public static string GetMongoDatabase(this string path)
    {
        return path.GetPathValue("db");
    }

    public static string GetMongoCollectionName(this string path)
    {
        return path.GetPathValue("collection");
    }

    #endregion

    public static string GetPathValue(this string path, string parameter)
    {
        foreach (var p in path.Split(';'))
        {
            if (!p.Contains("="))
            {
                continue;
            }

            int pos = p.IndexOf("=");
            var param = p.Substring(0, pos).Trim();
            if (param == parameter)
            {
                return p.Substring(pos + 1).Trim();
            }
        }

        return String.Empty;
    }

    public static bool TryParseCacheProvider(this string provider, out CacheProviderTypes providerType)
    {
        try
        {
            providerType = (CacheProviderTypes)Enum.Parse(typeof(CacheProviderTypes), provider, true);

            return true;
        }
        catch
        {
            providerType = CacheProviderTypes.Unknown;
            return false;
        }
    }
}
