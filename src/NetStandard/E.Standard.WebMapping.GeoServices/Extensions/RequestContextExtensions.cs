using System;
using System.Threading.Tasks;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Extensions;

static internal class RequestContextExtensions
{
    /// <summary>
    /// Runs <paramref name="requestAction"/> and - only if <see cref="IRequestContext.Trace"/>
    /// is enabled - logs <paramref name="requestBody"/> together with the request's result via
    /// <see cref="IGeoServiceRequestLogger"/> (kept as two separate fields rather than
    /// pre-concatenated, so structured loggers/trace tooling can keep a JSON response
    /// recognizable as such instead of it being buried behind a text prefix).
    /// Generic over <typeparamref name="T"/> so this also wraps the binary-returning
    /// <c>AgsAuthenticationHandler.TryGetRawAsync</c>/<c>TryPostRawAsync</c> calls, not just the
    /// text-returning ones - a binary response is logged as a short byte-count placeholder (see
    /// <see cref="DescribeResponseForLog{T}(T)"/>) rather than dumped as raw bytes.
    /// <paramref name="describeResponse"/> can override that default rendering, e.g. to decode a
    /// <c>byte[]</c> response that is actually text (such as an XML capabilities document fetched
    /// as raw bytes for parsing) back into readable text for the log/trace.
    /// </summary>
    async static public Task<T> LogRequest<T>(
        this IRequestContext requestContext,
        string server,
        string service,
        string requestBody,
        string method,
        Func<string, Task<T>> requestAction,
        Func<T, string> describeResponse = null
        )
    {
        var response = await requestAction(requestBody);

        if (requestContext.Trace)
        {
            requestContext.GetRequiredService<IGeoServiceRequestLogger>()
                .LogString(server, service, method, (describeResponse ?? DescribeResponseForLog)(response), requestBody);
        }

        return response;
    }

    /// <summary>
    /// Overload for GET-style requests that have no request body to log separately, so callers
    /// don't have to thread an unused/empty <c>requestBody</c> through the overload above.
    /// </summary>
    async static public Task<T> LogRequest<T>(
        this IRequestContext requestContext,
        string server,
        string service,
        string method,
        Func<Task<T>> requestAction,
        Func<T, string> describeResponse = null
        )
    {
        var response = await requestAction();

        if (requestContext.Trace)
        {
            requestContext.GetRequiredService<IGeoServiceRequestLogger>()
                .LogString(server, service, method, (describeResponse ?? DescribeResponseForLog)(response));
        }

        return response;
    }

    /// <summary>
    /// Overload for requests whose body comes from an <see cref="IRequestBuilder"/> (e.g.
    /// <c>BaseRequestBuilder&lt;T&gt;</c>). Builds the request exactly once here instead of
    /// requiring callers to call <c>requestBuilder.Build()</c> themselves at the call site.
    /// </summary>
    async static public Task<T> LogRequest<T>(
        this IRequestContext requestContext,
        string server,
        string service,
        IRequestBuilder requestBuilder,
        string method,
        Func<string, Task<T>> requestAction,
        Func<T, string> describeResponse = null
        )
        => await requestContext.LogRequest(server, service, requestBuilder.Build(), method, requestAction, describeResponse);

    /// <summary>
    /// Renders a request's result for the request-audit log/trace: text responses (the common
    /// case, typically JSON) are logged verbatim, binary responses (e.g. exported map images,
    /// attachment downloads) are logged as a short byte-count placeholder instead of dumping raw
    /// bytes into the log/trace backend.
    /// </summary>
    static private string DescribeResponseForLog<T>(T response)
        => response switch
        {
            string s => s,
            byte[] bytes => $"<binary response: {bytes.Length} bytes>",
            null => string.Empty,
            _ => response.ToString() ?? string.Empty
        };
}
