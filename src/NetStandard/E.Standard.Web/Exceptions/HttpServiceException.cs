using System;
using System.Net;

namespace E.Standard.Web.Exceptions;

public class HttpServiceException : Exception
{
    public HttpServiceException(HttpStatusCode statusCode, Exception? inner = null)
        : this(statusCode, $"Request returned Statuscode {statusCode}", inner)
    { }

    public HttpServiceException(HttpStatusCode statusCode, string message, Exception? inner = null)
        : base(message, inner)
    {
        this.StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; set; }
}
