using System;

namespace E.Standard.WebMapping.Core.Exceptions;

public class InfoException : Exception
{
    public InfoException(string message) : base(message) { }
}