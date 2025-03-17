using E.Standard.ThreadSafe;
using Portal.Core.Models.Caching;
using System;
using System.Linq;

namespace Portal.Core.AppCode.Services;

public class InMemoryPortalAppCache
{
    private ThreadSafeDictionary<string, CacheItem> _cache = new ThreadSafeDictionary<string, CacheItem>();
    private string[] _allCmsRoles = null;

    #region Users

    public string[] GetUserRoles(string userName)
    {
        userName = userName.ToLower();
        if (!_cache.ContainsKey(userName))
        {
            return null;
        }

        var item = _cache[userName];
        if (item == null)
        {
            return null;
        }

        if (item.Expires < DateTime.Now)
        {
            _cache.Remove(userName);
            return null;
        }

        return (string[])item.Data;
    }
    public void SetUserRoles(string userName, string[] roles)
    {
        userName = userName.ToLower();
        if (roles == null)
        {
            roles = new string[0];
        }

        _cache.Add(userName, new CacheItem(roles, 60 * 24));
    }
    public string[] GetUserNames()
    {
        return _cache.Keys.ToArray();
    }

    public string[] AllCmsRoles
    {
        get { return _allCmsRoles; }
        set { _allCmsRoles = value; }
    }

    #endregion

    public void Clear()
    {
        _cache.Clear();
        _allCmsRoles = null;
    }
}
