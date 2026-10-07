using E.Standard.Cms.Git.Services;

using Microsoft.Extensions.DependencyInjection;

namespace E.Standard.Cms.Git.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddCmsGitService(this IServiceCollection services)
        => services.AddSingleton<CmsGitService>();
}
