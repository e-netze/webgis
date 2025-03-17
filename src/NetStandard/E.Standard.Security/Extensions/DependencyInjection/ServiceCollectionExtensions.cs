using E.Standard.Security.Services.ApplicationSecurity;
using E.Standard.Security.Services.Database;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Security.Extensions.DependencyInjection
{
    static public class ServiceCollectionExtensions
    {
        [Obsolete("Use E.Standard.Security.App assembly")]
        static public ApplicationSecurityUserManagerBuilder AddAppliationSecurityUserManager(this IServiceCollection services)
        {
            services.AddScoped<ApplicationSecurityUserManager>();

            return new ApplicationSecurityUserManagerBuilder(services);
        }

        [Obsolete("Use E.Standard.Security.App assembly")]
        static public ApplicationSecurityUserManagerBuilder AddApplicationSecurityProvider<T>(this ApplicationSecurityUserManagerBuilder builder)
            where T : class, IApplicationSecurityProvider
        {

            builder.Services.AddTransient<IApplicationSecurityProvider, T>();

            return builder;
        }

        [Obsolete("Use E.Standard.Security.App assembly")]
        static public ApplicationSecurityUserManagerBuilder AddApplicationSecurityProvider<TProvider, TOptions>(this ApplicationSecurityUserManagerBuilder builder, Action<TOptions> setupOptions)
            where TProvider : class, IApplicationSecurityProvider
            where TOptions : class
        {
            builder.Services.Configure(setupOptions);
            builder.Services.AddTransient<IApplicationSecurityProvider, TProvider>();

            return builder;
        }

        [Obsolete("Use E.Standard.Security.Internal assembly")]
        static public IServiceCollection AddTicketDbService(this IServiceCollection services, Action<TicketDbServiceOptions> setupOptions)
        {
            services.Configure(setupOptions);
            services.AddTransient<TicketDbService>();

            return services;
        }
    }
}
