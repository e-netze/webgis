using E.Standard.Extensions.Compare;
using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cms.AppCode.Services;

public class CultureProvider : ICultureProvider
{
    private readonly string DefaultCulture = "en";
    public CultureProvider(IHttpContextAccessor httpContextAccessor, IOptions<MarkdownLocalizerOptions> localizerOptions)
    {
        DefaultCulture = localizerOptions?.Value?.DefaultLanguage.OrTake(DefaultCulture);

        if (httpContextAccessor?.HttpContext?.Request is not null)
        {
            Culture = httpContextAccessor.HttpContext.Request.Query["_ul"]
                .ToString()
                .OrTake(DefaultCulture);
        }

        if (string.IsNullOrEmpty(Culture))
        {
            Culture = DefaultCulture;
        }
    }

    public string Culture { get; set; }
}
