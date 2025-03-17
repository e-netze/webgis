using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Middleware;

public class XForwardedProtoMiddleware
{
    private readonly RequestDelegate _next;

    public XForwardedProtoMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var xProtoHeader = context.Request.Headers["X-Forwarded-Proto"].ToString();
        if (xProtoHeader != null && xProtoHeader.StartsWith("https", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.Scheme = "https";
        }

        //Console.WriteLine($"{ context.Request.Scheme }://{ context.Request.Host }/{ context.Request.Path }?{ context.Request.QueryString }");
        //foreach(var header in context.Request.Headers)
        //{
        //    Console.WriteLine($" HEADER: { header.Key }={ header.Value }");
        //}

        await _next.Invoke(context);
    }
}
