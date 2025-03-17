using E.Standard.Web.Exceptions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Exceptions;
using System.Net;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;

static internal class ExceptionExtensions
{
    static public void ThrowIfTokenRequired(this WebException ex)
    {
        if (ex.Message.Contains("(403)") ||
            ex.Message.Contains("(498)") ||
            ex.Message.Contains("(499)"))
        {
            throw new TokenRequiredException();
        }
    }

    static public void ThrowIfTokenRequired(this HttpServiceException httpEx)
    {
        if (httpEx.StatusCode == HttpStatusCode.Forbidden /* 403 */ ||
           (int)httpEx.StatusCode == 498 ||
           (int)httpEx.StatusCode == 499)
        {
            throw new TokenRequiredException();
        }
    }
}
