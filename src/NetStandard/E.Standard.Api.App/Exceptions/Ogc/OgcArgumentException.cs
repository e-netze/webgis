using System;

namespace E.Standard.Api.App.Exceptions.Ogc;

public class OgcArgumentException : Exception
{
    public OgcArgumentException(string message, Exception inner = null)
        : base(message, inner)
    {
    }
}
