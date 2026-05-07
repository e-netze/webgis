using System.Collections.Specialized;

using Microsoft.AspNetCore.Http;

namespace E.Standard.Api.App.Extensions;

static public class RequestExtensions
{
    static public NameValueCollection FormCollection(this HttpRequest request)
    {
        return request != null && request.HasFormContentType && request.Form != null ?
               request.Form.ToCollection() : new NameValueCollection();
    }
}
