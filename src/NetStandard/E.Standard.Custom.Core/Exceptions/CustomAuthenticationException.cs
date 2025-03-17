using System;

namespace E.Standard.Custom.Core.Exceptions;

public class CustomAuthenticationException : Exception
{
    public CustomAuthenticationException(string message)
        : base(message)
    {

    }
}
