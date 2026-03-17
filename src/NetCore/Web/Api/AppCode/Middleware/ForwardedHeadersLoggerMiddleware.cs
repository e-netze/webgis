using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RTools.Util;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Middleware;

public class ForwardedHeadersLoggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ForwardedHeadersLoggerMiddleware> _logger;

    public ForwardedHeadersLoggerMiddleware(RequestDelegate next, ILogger<ForwardedHeadersLoggerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation(
                        "Scheme={Scheme}, Host={Host}, Path={Path}, XFP={XFP}, XFHost={XFHost}",
                        context.Request.Scheme,
                        context.Request.Host,
                        context.Request.Path,
                        context.Request.Headers["X-Forwarded-Proto"].ToString(),
                        context.Request.Headers["X-Forwarded-Host"].ToString());

        await _next(context);
    }
}
