using System;

namespace E.Standard.Api.App.Exceptions.Ogc;

public class OgcNotAuthorizedException : Exception
{
    public OgcNotAuthorizedException(string message, Exception inner = null)
        : base(message, inner)
    {
    }
}
