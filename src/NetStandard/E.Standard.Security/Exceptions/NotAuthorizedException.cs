using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace E.Standard.Security.Exceptions
{
    [Obsolete("Use E.Standard.Security.App assembly")]
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
            : this($"User { claimsPrincipal?.Identity?.Name ?? "unknown username" } is not authorized")
        {
        }
    }
}
