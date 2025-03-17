using E.Standard.Web.Abstractions;
using E.Standard.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Web.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddHttpService<T>(this IServiceCollection services, Action<HttpServiceOptions> configureAction)
        where T : class, IHttpService
    {
        services.Configure<HttpServiceOptions>(configureAction);
        return services.AddTransient<IHttpService, T>();
    }
}
