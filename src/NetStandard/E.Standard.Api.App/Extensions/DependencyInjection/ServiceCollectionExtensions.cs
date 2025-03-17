using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cms;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Api.App.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddCmsDocumentsService(this IServiceCollection services,
                                                            Action<CmsDocumentsServiceOptions> configureOptions)
    {
        services.Configure<CmsDocumentsServiceOptions>(configureOptions);
        // Singelton, weil in CacheService injectet
        services.AddSingleton<CmsDocumentsService>();

        return services;
    }

    static public IServiceCollection AddMapServiceIntitializerService(this IServiceCollection services)
    {
        return services.AddTransient<MapServiceInitializerService>();
    }

    static public IServiceCollection AddLookupService(this IServiceCollection services)
    {
        return services.AddTransient<LookupService>();
    }
}
