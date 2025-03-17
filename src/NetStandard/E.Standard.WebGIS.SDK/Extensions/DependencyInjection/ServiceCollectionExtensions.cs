using E.Standard.WebGIS.SDK.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.WebGIS.SDK.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddSDKPluginManagerService(this IServiceCollection services, Action<SDKPluginManagerServiceOptions> configureOpitons)
    {
        services.Configure<SDKPluginManagerServiceOptions>(configureOpitons);

        // Singleton, damit nicht jedesmal das Verzeichnis mit den Plugins durchsucht wird!
        services.AddSingleton<SDKPluginManagerService>();

        return services;
    }
}
