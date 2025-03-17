using Api.Core.AppCode.Services.Ogc;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Caching.Abstraction;
using E.Standard.Caching.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services;

public class CacheClearService
{
    private readonly OgcRequestService _ogcRequest;
    private readonly CacheService _cache;
    private readonly KeyValueCacheService _keyValueCache;
    private readonly ITempDataByteCache _tempDataCache;
    private readonly IEnumerable<IExpectableUserRoleNamesProvider> _expectableUserRolesNamesProviders;
    private readonly IEnumerable<ICacheClearableService> _cacheClearableServices;

    public CacheClearService(OgcRequestService ogcRequest,
                             CacheService cache,
                             ITempDataByteCache tempDataCache,
                             KeyValueCacheService keyValueCache,
                             IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                             IEnumerable<ICacheClearableService> cacheClearableServices)
    {
        _ogcRequest = ogcRequest;
        _cache = cache;
        _keyValueCache = keyValueCache;
        _tempDataCache = tempDataCache;
        _expectableUserRolesNamesProviders = expectableUserRolesNamesProviders;
        _cacheClearableServices = cacheClearableServices;
    }

    async public Task<bool> ClearCache(string id)
    {
        foreach (var cacheClearable in _cacheClearableServices)
        {
            await cacheClearable.Clear();
        }

        if (_keyValueCache?.KeyValueCacheAsideInstance is ICacheClearable)
        {
            await ((ICacheClearable)_keyValueCache?.KeyValueCacheAsideInstance).Clear();
        }

        if (String.IsNullOrWhiteSpace(id) || id.StartsWith("@@") == false)
        {
            _cache.Clear(_expectableUserRolesNamesProviders, id);
        }

        if (String.IsNullOrWhiteSpace(id) || id == "@@ogc")
        {
            _ogcRequest.ClearCache();
        }

        await _tempDataCache.Clear();

        return true;
    }

    public void OgcClearCache()
    {
        _ogcRequest.ClearCache();
    }

    public string LastInitErrorMessage => _cache.LastInitErrorMessage;
}
