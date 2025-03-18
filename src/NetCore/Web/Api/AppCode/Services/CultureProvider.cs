using Api.Core.AppCode.Extensions;
using E.Standard.Localization.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Api.Core.AppCode.Services;

public class CultureProvider : ICultureProvider
{
    private string DefaultCulture = "de";
    public CultureProvider(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor?.HttpContext?.Request is not null)
        {
            Culture = httpContextAccessor.HttpContext.Request.QueryOrForm("_ul");
        }

        if (string.IsNullOrEmpty(Culture))
        {
            Culture = DefaultCulture;
        }
    }
    public string Culture { get; set; } = "en";
}
