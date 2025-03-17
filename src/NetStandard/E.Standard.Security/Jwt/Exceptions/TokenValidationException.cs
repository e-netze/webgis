using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Jwt.Exceptions
{
    [Obsolete("Use E.Standard.Security.Core assembly")]
    public class TokenValidationException : Exception
    {
        public TokenValidationException(string message) :
            base($"Token validation: { message }")
        {

        }
    }
}
