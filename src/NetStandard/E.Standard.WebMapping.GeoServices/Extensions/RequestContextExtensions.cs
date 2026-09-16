using System;
using System.Threading.Tasks;

using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Extensions;

static internal class RequestContextExtensions
{
    async static public Task<string> LogRequest(
        this IRequestContext requestContext,
        string server,
        string service,
        string requestBody,
        string method,
        Func<string, Task<string>> requestAction
        )
    {
        var response = await requestAction(requestBody);

        if (requestContext.Trace)
        {
            // requestBody and response are passed separately (not pre-concatenated), so
            // structured loggers/trace tooling can keep the response - typically JSON -
            // recognizable as such instead of it being buried behind a text prefix.
            requestContext.GetRequiredService<IGeoServiceRequestLogger>()
                .LogString(server, service, method, response, requestBody);
        }

        return response;
    }
}
