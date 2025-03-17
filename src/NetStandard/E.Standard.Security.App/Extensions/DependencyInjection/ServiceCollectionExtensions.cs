using E.Standard.Security.App.Services;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Security.App.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public ApplicationSecurityUserManagerBuilder AddAppliationSecurityUserManager(this IServiceCollection services)
    {
        services.AddScoped<ApplicationSecurityUserManager>();

        return new ApplicationSecurityUserManagerBuilder(services);
    }

    static public ApplicationSecurityUserManagerBuilder AddApplicationSecurityProvider<T>(this ApplicationSecurityUserManagerBuilder builder)
        where T : class, IApplicationSecurityProvider
    {

        builder.Services.AddTransient<IApplicationSecurityProvider, T>();

        return builder;
    }

    static public ApplicationSecurityUserManagerBuilder AddApplicationSecurityProvider<TProvider, TOptions>(this ApplicationSecurityUserManagerBuilder builder, Action<TOptions> setupOptions)
        where TProvider : class, IApplicationSecurityProvider
        where TOptions : class
    {
        builder.Services.Configure(setupOptions);
        builder.Services.AddTransient<IApplicationSecurityProvider, TProvider>();

        return builder;
    }

    static public IServiceCollection AddBotDetection(this IServiceCollection services)
    {
        return services.AddSingleton<BotDetectionService>();
    }
}
