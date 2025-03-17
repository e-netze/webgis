using System;

namespace E.Standard.Security.Core.Excetions;

public class SecurityException : Exception
{
    public SecurityException(string message = "")
        : base(message)
    {

    }
}
