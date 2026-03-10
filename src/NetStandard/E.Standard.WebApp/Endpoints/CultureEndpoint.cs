using E.Standard.WebApp.Abstraction;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Globalization;

namespace E.Standard.WebApp.Endpoints;

internal class CultureEndpoint : IApiEndpoint
{
    public void Register(IEndpointRouteBuilder app)
    {
        app.MapGet("/instance/_culture",
        () => new
        {
            culture = CultureInfo.CurrentCulture.Name,
            cultureDisplayName = CultureInfo.CurrentCulture.DisplayName,
            cultureEnglishName = CultureInfo.CurrentCulture.EnglishName,
            cultureUI = CultureInfo.CurrentCulture.Name,
            currentTimeString = DateTime.Now.ToString()
        });
    }
}
