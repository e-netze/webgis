using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Api.Core.AppCode.Middleware;

/// <summary>
/// Opens an <see cref="ILogger.BeginScope{TState}(TState)"/> for the whole request, so every log
/// line written while handling it - including the WebGIS performance/usage/exception loggers (see
/// <c>Api.Core.AppCode.Services.Logging</c>) - carries the same request correlation id, path and
/// method without having to repeat it in every individual log message. Scope values are picked up
/// automatically by the OpenTelemetry log export (<c>IncludeScopes = true</c>, see
/// <c>webgis.ServiceDefaults</c>) as well as by any other scope-aware logging provider.
/// </summary>
public class RequestLoggingScopeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingScopeMiddleware> _logger;

    public RequestLoggingScopeMiddleware(RequestDelegate next, ILogger<RequestLoggingScopeMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var scopeState = new Dictionary<string, object>
        {
            ["RequestId"] = context.TraceIdentifier,
            ["RequestPath"] = context.Request.Path.Value ?? string.Empty,
            ["RequestMethod"] = context.Request.Method,
        };

        using (_logger.BeginScope(scopeState))
        {
            await _next(context);
        }
    }
}
