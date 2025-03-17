using System;

namespace E.Standard.Security.App.Exceptions;

public class UnknownUsernameException : Exception
{
    public UnknownUsernameException() : base("Unknown username")
    {
    }
}
