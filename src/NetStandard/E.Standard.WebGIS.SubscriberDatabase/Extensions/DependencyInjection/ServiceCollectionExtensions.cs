using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.WebGIS.SubscriberDatabase.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    static public IServiceCollection AddSubscriberDatabaseService(this IServiceCollection services, Action<SubscriberDatabaseServiceOptions> setupOptions)
    {
        services.Configure(setupOptions);
        services.AddTransient<SubscriberDatabaseService>();
        services.AddTransient<MigrateSubscriberDatabaseService>();

        return services;
    }
}
