using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Exceptions
{
    [Obsolete("Use E.Standard.Security.App assembly")]
    public class UnknownUsernameException : Exception
    {
        public UnknownUsernameException() : base("Unknown username")
        {
        }
    }
}
