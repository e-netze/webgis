using System;

namespace E.Standard.Security.App.Exceptions;

public class WrongPasswordException : Exception
{
    public WrongPasswordException() : base("Wrong password")
    {
    }
}
