using System;

namespace E.Standard.Api.App.Exceptions.Ogc;

public class OgcException : Exception
{
    public OgcException(string message, Exception inner = null)
        : base(message, inner)
    {
    }
}
