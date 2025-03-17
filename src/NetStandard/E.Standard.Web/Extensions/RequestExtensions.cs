using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;

namespace E.Standard.Web.Extensions;

public static class RequestExtensions
{
    static public NameValueCollection HeadersCollection(this HttpRequest request)
    {
        var headersCollection = new NameValueCollection();

        if (request?.Headers != null)
        {
            foreach (var headerKey in request.Headers.Keys)
            {
                headersCollection[headerKey] = request.Headers[headerKey];
            }
        }

        return headersCollection;
    }
}
