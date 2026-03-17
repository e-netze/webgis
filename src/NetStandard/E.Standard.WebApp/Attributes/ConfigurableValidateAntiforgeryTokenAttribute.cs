using E.Standard.WebApp.Options;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace E.Standard.WebApp.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ConfigurableValidateAntiforgeryTokenAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var options = context.HttpContext.RequestServices
            .GetService<IOptions<SecurityOptions>>()?
            .Value;

        if (options?.DisableAntiforgery == true)
        {
            return;
        }

        var antiforgery = context.HttpContext.RequestServices
            .GetRequiredService<IAntiforgery>();

        await antiforgery.ValidateRequestAsync(context.HttpContext);
    }
}
