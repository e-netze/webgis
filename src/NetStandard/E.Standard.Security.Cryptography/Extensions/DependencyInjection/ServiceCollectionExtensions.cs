using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Security.Cryptography.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddCrytpographyService<T>(this IServiceCollection services, Action<CryptoServiceOptions> configAction)
        where T : class, ICryptoService
    {
        Console.WriteLine($"Adding custom service: <ICryptoService, {typeof(T)}>");

        services.Configure<CryptoServiceOptions>(configAction);
        services.AddTransient<ICryptoService, T>();

        services.AddSingleton<JwtAccessTokenService>();

        return services;
    }
}
