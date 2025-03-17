using Api.Core.AppCode.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Json;
using E.Standard.WebMapping.Core;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware;

public class OriginalUrlParamterMiddleware
{
    private readonly RequestDelegate _next;

    public OriginalUrlParamterMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    async public Task Invoke(HttpContext httpContext,
                             HttpRequestContextService requestContext)
    {
        var urlParamtersJsonString = httpContext.Request.FormOrQuery("_original_url_parameters");
        if (!string.IsNullOrEmpty(urlParamtersJsonString))
        {
            requestContext.OriginalUrlParameters = new RequestParameters(JSerializer.Deserialize<Dictionary<string, string>>(urlParamtersJsonString));
        }

        await _next(httpContext);
    }
}

