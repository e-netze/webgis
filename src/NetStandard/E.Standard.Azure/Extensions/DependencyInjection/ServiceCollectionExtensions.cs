using E.Standard.Azure.Services.Parser;
using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace E.Standard.Azure.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddAzureKeyVaultConfigValueParser(this IServiceCollection services, Action<AzureKeyVaultConfigValueParserOptions> optionsAction)
    {
        services.Configure(optionsAction);
        services.AddTransient<IConfigValueParser, AzureKeyVaultConfigValueParser>();

        return services;
    }
}
