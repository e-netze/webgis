using System;

namespace E.Standard.Security.Core.Jwt.Exceptions;

public class TokenValidationException : Exception
{
    public TokenValidationException(string message) :
        base($"Token validation: {message}")
    {

    }
}
