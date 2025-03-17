using System;

namespace E.Standard.Api.App.Exceptions;

public class QueryEngineInformationException : Exception
{
    public QueryEngineInformationException(string warning)
        : base(warning)
    {

    }
}
