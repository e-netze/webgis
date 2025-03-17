using System;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Exceptions;

public class TokenRequiredException : Exception
{
    public TokenRequiredException() { }
    public TokenRequiredException(string message) : base(message)
    {
    }
}

public class OperationException : Exception
{
    public OperationException(string message) : base(message)
    {
    }
}

public class GatewayTimeoutException : Exception
{
    public GatewayTimeoutException(string message) : base(message)
    {
    }
}
