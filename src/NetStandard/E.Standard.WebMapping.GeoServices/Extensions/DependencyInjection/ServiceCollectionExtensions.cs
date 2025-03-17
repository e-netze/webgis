using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace E.Standard.WebMapping.GeoServices.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddGeoServiceDependencies(this IServiceCollection services)
        => services
            .AddTransient<TestServiceProviderLivetimeService>()
            .AddSingleton<AgsTokenStore>()
            .AddTransient<AgsAuthenticationHandler>();
}
