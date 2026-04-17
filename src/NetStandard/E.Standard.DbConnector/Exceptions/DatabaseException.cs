using System;

using E.Standard.Extensions.Abstractions;

namespace E.Standard.DbConnector.Exceptions;

public class DatabaseException : Exception, IGenericExceptionMessage
{
    public DatabaseException(string message, Exception innerException = null)
     : base(message, innerException) { }

    public string GenericMessage => "A database error occoured";
}
