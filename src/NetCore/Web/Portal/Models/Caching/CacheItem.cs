using System;

namespace Portal.Core.Models.Caching;

public class CacheItem
{
    public CacheItem(object data, double minutes = 60)
    {
        this.Expires = DateTime.Now.AddMinutes(minutes);
        this.Data = data;
    }

    public string Name { get; private set; }
    public DateTime Expires { get; private set; }

    public object Data { get; private set; }
}
