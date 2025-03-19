using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using System;
using System.Threading.Tasks;

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
            requestContext.GetRequiredService<IGeoServiceRequestLogger>()
                .LogString(server, service, method,
                $"{requestBody}{Environment.NewLine}=>{Environment.NewLine}{response}");
        }

        return response;
    }
}
