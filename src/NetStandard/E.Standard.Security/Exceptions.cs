using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security
{
    [Obsolete("Use E.Standard.Security.Core assembly")]
    public class SecurityException : Exception
    {
        public SecurityException(string message="")
            :base(message)
        {

        }
    }
}
