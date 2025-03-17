using E.Standard.Cms.Configuration.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Cms.Configuration.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddCmsConfigurationService(this IServiceCollection services, Action<CmsConfigurationServiceOptions> configAction)
    {
        services.Configure<CmsConfigurationServiceOptions>(configAction);
        return services.AddSingleton<CmsConfigurationService>();
    }
}
