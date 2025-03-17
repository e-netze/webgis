using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Exceptions
{
    [Obsolete("Use E.Standard.Security.Internal assembly")]
    public class TicketDbException : Exception
    {
        public TicketDbException(string username, string message)
            : base(message)
        {
            Username = username;
        }

        public readonly string Username;
    }
}
