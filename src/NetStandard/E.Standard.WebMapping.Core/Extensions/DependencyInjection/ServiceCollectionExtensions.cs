using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace E.Standard.WebMapping.Core.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddRequestContextService(this IServiceCollection services)
            => services.AddScoped<IRequestContext, RequestContextService>();
}
