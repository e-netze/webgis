using E.Standard.Configuration.Services;
using E.Standard.Configuration.Services.Parser;
using E.Standard.Security.App.Json;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Configuration.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddHostingEnvironmentConfigValueParser(this IServiceCollection services)
    {
        services.AddTransient<IConfigValueParser, HostingEnvironmentConfigValueParser>();
        return services;
    }

    static public IServiceCollection AddApplicationSecurityConfiguration(this IServiceCollection services)
    {
        //services.Configure<ApplicationSecurityConfigOptions>(configuration.GetSection("ApplicationSecurity"));

        services.AddOptions<ApplicationSecurityConfig>()
             .Configure(applicationSecurityConfig =>
             {
                 Console.WriteLine("AddApplicationSecurityConfiguration");
                 Console.WriteLine("Try Load Josn File");

                 var appConfig = new JsonAppConfiguration("application-security.config");

                 if (appConfig.Exists)
                 {
                     applicationSecurityConfig.MemberwiseCopy(appConfig.Deserialize<ApplicationSecurityConfig>());
                     applicationSecurityConfig.UseApplicationSecurity = true;
                 }
                 else
                 {
                     applicationSecurityConfig.UseApplicationSecurity = false;
                 }
             });

        return services;
    }

    static public IServiceCollection AddConfiguraionService(this IServiceCollection services)
    {
        services.AddTransient<ISecurityConfigurationService, SecurityConfigurationService>();
        services.AddSingleton<ConfigurationService>();

        return services;
    }
}
