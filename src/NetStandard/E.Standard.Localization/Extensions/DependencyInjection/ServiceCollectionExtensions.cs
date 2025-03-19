using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Services;
using Microsoft.Extensions.Localization;

namespace Microsoft.Extensions.DependencyInjection;
static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddMarkdownLocalizerFactory<TCultureProvider>(
            this IServiceCollection services,
            Action<MarkdownLocalizerOptions> setupAction
        )
        where TCultureProvider : class, ICultureProvider
    {
        return services
            .Configure(setupAction)
            .AddTransient<ICultureProvider, TCultureProvider>()
            .AddTransient<IMarkdownLocationInitializer, MarkdownLocalizerInitializer>()
            .AddTransient<IStringLocalizerFactory, MarkdownLocalizerFactory>();
    }
}
