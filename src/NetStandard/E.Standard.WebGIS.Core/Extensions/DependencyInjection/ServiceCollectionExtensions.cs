using E.Standard.WebGIS.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.WebGIS.Core.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddSpatialReferenceService(this IServiceCollection services, Action<SpatialReferenceServiceOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddTransient<SpatialReferenceService>();

        return services;
    }

    static public IServiceCollection AddFileTracerService(this IServiceCollection services, Action<FileTracerServiceOptions> configureOptions)
    {
        services.Configure<FileTracerServiceOptions>(configureOptions);
        services.AddTransient<ITracerService, FileTracerService>();

        return services;
    }

    static public IServiceCollection AddNullTracerService(this IServiceCollection services)
    {
        return services.AddSingleton<ITracerService, NullTracerService>();
    }

    static public IServiceCollection AddConsoleTracerService(this IServiceCollection services, Action<ConsoleTracerServiceOptions> configureOptions)
    {
        services.Configure<ConsoleTracerServiceOptions>(configureOptions);
        services.AddTransient<ITracerService, ConsoleTracerService>();

        return services;
    }

    static public IServiceCollection AddTracerService<TService, TOptions>(this IServiceCollection services, Action<TOptions> configureOptions)
        where TService : class, ITracerService
        where TOptions : class
    {
        services.Configure<TOptions>(configureOptions);
        services.AddTransient<ITracerService, TService>();

        return services;
    }

    static public IServiceCollection AddGlobalisationService(this IServiceCollection services, Action<GlobalisationServiceOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddSingleton<IGlobalisationService, GlobalisationService>();

        return services;
    }
}
