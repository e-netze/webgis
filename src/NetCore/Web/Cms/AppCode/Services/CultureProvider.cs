using E.Standard.Extensions.Compare;
using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Services;
using System;
using System.Linq;
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
            var request = httpContextAccessor.HttpContext.Request;
            var cookieLanguage = request.Cookies["cms-language"];
            var supportedLanguages = localizerOptions?.Value?.SupportedLanguages;

            if (!string.IsNullOrEmpty(cookieLanguage)
                && supportedLanguages?.Contains(cookieLanguage, StringComparer.OrdinalIgnoreCase) != true)
            {
                cookieLanguage = null;
            }

            Culture = request.Query["_ul"]
                .ToString()
                .OrTake(cookieLanguage ?? DefaultCulture);
        }

        if (string.IsNullOrEmpty(Culture))
        {
            Culture = DefaultCulture;
        }
    }

    public string Culture { get; set; }
}
