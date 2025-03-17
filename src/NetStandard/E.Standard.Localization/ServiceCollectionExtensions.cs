using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace E.Standard.Localization;
static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddMarkdownLocalizerFactory<TCultureProvider>(this IServiceCollection services)
        where TCultureProvider : class, ICultureProvider
    {
        return services
            .AddTransient<ICultureProvider, TCultureProvider>()
            .AddTransient<IStringLocalizerFactory, MarkdownLocalizerFactory>();
    }
}
