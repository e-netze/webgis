using E.Standard.WebApp.Services;

using Microsoft.Extensions.DependencyInjection;

namespace E.Standard.WebApp.Extensions.DependencyInjection;

static public class BrandStyleServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="BrandStyleService"/> that renders brand CSS custom
    /// properties from environment variables (e.g. CSS_WEBGIS_BRAND_PRIMARY), used to
    /// override the values from "site.overrides.css" in containerized deployments.
    /// </summary>
    static public IServiceCollection AddBrandStyleService(this IServiceCollection services)
        => services.AddSingleton<BrandStyleService>();
}
