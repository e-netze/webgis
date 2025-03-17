using E.Standard.ActiveDirectory;
using E.Standard.Caching.Abstraction;
using E.Standard.Json;
using Newtonsoft.Json;
using System;
using System.IO;

namespace E.Standard.Caching.FileSystem;

public class FileSystemCache : IKeyValueCache
{
    private string _rootPath = String.Empty;
    private readonly Impersonator _impersonateor;

    private static readonly object _locker = new object();

    public FileSystemCache()
    {
        _impersonateor = new Impersonator(String.Empty);
    }

    public FileSystemCache(string impersonateString)
    {
        _impersonateor = new Impersonator(impersonateString);
    }

    #region IKeyValueCache

    public bool Init(string initalParameter)
    {
        _rootPath = initalParameter;

        using (var impersonateContext = _impersonateor.ImpersonateContext(true))
        {
            DirectoryInfo di = new DirectoryInfo(_rootPath);

            if (!di.Exists)
            {
                di.Create();
            }

            DeleteExpiredItems();
        }

        return true;
    }

    public string Get(string key)
    {
        try
        {
            using (var impersonateContext = _impersonateor.ImpersonateContext(true))
            {
                FileInfo fi = new FileInfo(
                    Path.Combine(_rootPath, Key2Filename(key)));

                if (!fi.Exists)
                {
                    throw new Exception("Not exists");
                }

                string jsonString = File.ReadAllText(fi.FullName);
                var item = JSerializer.Deserialize<JsonItemClass>(jsonString);

                if (item.IsExpired)
                {
                    Remove(key);
                    throw new Exception("Item expired");
                }

                return item.Value;
            }
        }
        catch
        {
            return null;
        }
    }

    public void Remove(string key)
    {
        try
        {
            lock (_locker)
            {
                using (var impersonateContext = _impersonateor.ImpersonateContext(true))
                {
                    FileInfo fi = new FileInfo(
                        Path.Combine(_rootPath, Key2Filename(key)));

                    if (fi.Exists)
                    {
                        fi.Delete();
                    }
                }
            }
        }
        catch { }
    }

    public void Set(string key, object o, double expiresSeconds)
    {
        var item = new JsonItemClass(key, o.ToString(), expiresSeconds);

        var jsonString = JSerializer.Serialize(item);

        lock (_locker)
        {
            using (var impersonateContext = _impersonateor.ImpersonateContext(true))
            {
                FileInfo fi = new FileInfo(
                    Path.Combine(_rootPath, Key2Filename(key)));

                if (fi.Exists)
                {
                    fi.Delete();
                }

                File.WriteAllText(fi.FullName, jsonString);
            }
        }
    }

    public int MaxChunkSize => int.MaxValue;

    #endregion

    #region Helper

    private string Key2Filename(string username)
    {
        return username.Replace(":", "~").Replace(@"\", "$") + ".json";
    }

    private void DeleteExpiredItems()
    {
        lock (_locker)
        {
            using (var impersonateContext = _impersonateor.ImpersonateContext(true))
            {
                foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.json"))
                {
                    try
                    {
                        string jsonString = File.ReadAllText(fi.FullName);
                        var item = JSerializer.Deserialize<JsonItemClass>(jsonString);

                        if (item.IsExpired)
                        {
                            fi.Delete();
                        }
                    }
                    catch { }
                }
            }
        }
    }

    #endregion

    #region Classes

    private class JsonItemClass
    {
        public JsonItemClass()
        {

        }

        public JsonItemClass(string cacheKey, string cacheVal, double expiresSeconds)
        {
            this.Key = cacheKey;
            this.Value = cacheVal;

            this.CreatedTicks = DateTime.UtcNow.Ticks;
            this.ExpiredTicks = DateTime.UtcNow.AddSeconds(expiresSeconds).Ticks;
        }

        [JsonProperty(PropertyName = "key")]
        [System.Text.Json.Serialization.JsonPropertyName("key")]
        public string Key { get; set; }
        [JsonProperty(PropertyName = "val")]
        [System.Text.Json.Serialization.JsonPropertyName("val")]
        public string Value { get; set; }
        [JsonProperty(PropertyName = "created")]
        [System.Text.Json.Serialization.JsonPropertyName("created")]
        public long CreatedTicks { get; set; }
        [JsonProperty(PropertyName = "expires")]
        [System.Text.Json.Serialization.JsonPropertyName("expires")]
        public long ExpiredTicks { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime CreationTime
        {
            get
            {
                return new DateTime(this.CreatedTicks, DateTimeKind.Utc);
            }
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime ExpireTime
        {
            get
            {
                return new DateTime(this.ExpiredTicks, DateTimeKind.Utc);
            }
        }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public bool IsExpired
        {
            get
            {
                return (DateTime.UtcNow > this.ExpireTime);
            }
        }
    }

    #endregion
}
