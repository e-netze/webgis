using Api.Core.AppCode.Reflection;
using Api.Core.AppCode.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware;

public class EtagMiddleware
{
    private readonly RequestDelegate _next;

    public EtagMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context,
                             RoutingEndPointReflectionService endpointReflection,
                             EtagService etag)
    {
        var etagAttribute = endpointReflection.GetCustomAttribute<EtagAttribute>();

        if (etagAttribute == null)
        {
            await _next(context);
        }
        else
        {
            if (etag.IfMatch(context))
            {
                if (etagAttribute != null)
                {
                    context.Response.StatusCode = 304;
                    return;
                }
            }

            if (etagAttribute.AppendResponseHeaders)
            {
                context.Response.OnStarting(state =>
                {
                    var context = (HttpContext)state;
                    if (context.Response.StatusCode == 200)
                    {
                        etag.AppendEtag(context, DateTime.Now.AddDays(etagAttribute.ExpirationDays));
                    }
                    return Task.CompletedTask;
                }, context);
            }

            await _next(context);


        }
    }
}
