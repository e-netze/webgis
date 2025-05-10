using Api.Core.AppCode.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Api.Core.AppCode.Extensions.DependencyInjection;

static public class ApplicationBuilderExtensions
{
    static public IApplicationBuilder AddEtagHandling(this IApplicationBuilder app)
        => app.UseMiddleware<EtagMiddleware>();

    static public IApplicationBuilder AddOriginalUrlParametersMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<OriginalUrlParamterMiddleware>();

    static public IApplicationBuilder AddApiExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<ApiExceptionMiddleware>();

}
