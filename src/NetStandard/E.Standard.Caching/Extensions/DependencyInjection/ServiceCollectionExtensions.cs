using E.Standard.Caching.Abstraction;
using E.Standard.Caching.FileSystem;
using E.Standard.Caching.InApp;
using E.Standard.Caching.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Caching.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddKeyValueCacheService(this IServiceCollection services, Action<KeyValueCacheServiceOptions> setupOptions)
    {
        services.Configure(setupOptions);
        services.AddSingleton<KeyValueCacheService>(); // Must be singletion!

        return services;
    }

    static public IServiceCollection AddFileSystemTempDataCache(this IServiceCollection services,
                                                                Action<FileSystemTempDataByteCacheOptions> configureOptions)
    {
        services.Configure<FileSystemTempDataByteCacheOptions>(configureOptions);
        services.AddSingleton<ITempDataByteCache, FileSystemTempDataByteCache>();
        return services;
    }

    static public IServiceCollection AddInAppTempDataObjectCache(this IServiceCollection services)
    {
        services.AddSingleton<ITempDataObjectCache, InAppTempDataObjectCache>();
        return services;
    }
}
