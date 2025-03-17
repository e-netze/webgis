using System;
using System.Security.Claims;

namespace E.Standard.Security.App.Exceptions;

public class NotAuthorizedException : Exception
{
    public NotAuthorizedException()
        : base()
    {

    }

    public NotAuthorizedException(string message)
        : base(message)
    {

    }

    public NotAuthorizedException(ClaimsPrincipal claimsPrincipal)
        : this($"User {claimsPrincipal?.Identity?.Name ?? "unknown username"} is not authorized")
    {
    }
}
