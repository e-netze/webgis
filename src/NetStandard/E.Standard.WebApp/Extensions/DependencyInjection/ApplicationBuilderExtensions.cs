using E.Standard.WebApp.Middleware;

using Microsoft.AspNetCore.Builder;

namespace E.Standard.WebApp.Extensions.DependencyInjection;

static public class ApplicationBuilderExtensions
{
    static public IApplicationBuilder AddEndpointAuthorizationMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<EndpointAuthorizationMiddleware>();
}
