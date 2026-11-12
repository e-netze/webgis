using Api.Core.AppCode.Extensions;

using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using System;
using System.Linq;

namespace Api.Core.AppCode.Services;

public class CultureProvider : ICultureProvider
{
    // written by the language switch of the html pages (_Layout.cshtml)
    public const string CultureCookieName = "webgis-api-ul";

    private string DefaultCulture = "de";

    public CultureProvider(
                IHttpContextAccessor httpContextAccessor,
                IOptions<MarkdownLocalizerOptions> options)
    {
        var request = httpContextAccessor?.HttpContext?.Request;
        var supportedLanguages = options?.Value?.SupportedLanguages ?? [];

        if (request is not null)
        {
            Culture = request.QueryOrForm("_ul");

            if (string.IsNullOrEmpty(Culture))
            {
                Culture = SupportedOrNull(request.Cookies[CultureCookieName], supportedLanguages);
            }

            if (string.IsNullOrEmpty(Culture))
            {
                Culture = FromAcceptLanguage(request, supportedLanguages);
            }
        }

        if (string.IsNullOrEmpty(Culture))
        {
            Culture = DefaultCulture;
        }
    }

    public string Culture { get; set; } = "en";

    private static string SupportedOrNull(string language, string[] supportedLanguages)
        => !string.IsNullOrEmpty(language) && supportedLanguages.Contains(language, StringComparer.OrdinalIgnoreCase)
            ? language.ToLowerInvariant()
            : null;

    private static string FromAcceptLanguage(HttpRequest request, string[] supportedLanguages)
    {
        var languages = request.GetTypedHeaders().AcceptLanguage;
        if (languages is null)
        {
            return null;
        }

        return languages
            .Where(l => (l.Quality ?? 1.0) > 0 && l.Value.HasValue)
            .OrderByDescending(l => l.Quality ?? 1.0)
            .Select(l => SupportedOrNull(l.Value.Value.Split('-')[0], supportedLanguages))
            .FirstOrDefault(l => l is not null);
    }
}
