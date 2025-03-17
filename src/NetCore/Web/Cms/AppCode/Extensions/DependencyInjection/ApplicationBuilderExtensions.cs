using Cms.AppCode.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Cms.AppCode.Extensions.DependencyInjection;

static public class ApplicationBuilderExtensions
{
    static public IApplicationBuilder AddXForwardedProtoMiddleware(this IApplicationBuilder appBuilder)
    {
        appBuilder.UseMiddleware<XForwardedProtoMiddleware>();

        return appBuilder;
    }
}
