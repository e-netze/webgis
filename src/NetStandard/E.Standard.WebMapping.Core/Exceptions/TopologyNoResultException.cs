using System;

namespace E.Standard.WebMapping.Core.Exceptions;

public class TopologyNoResultException : Exception
{
    public TopologyNoResultException(string message)
        : base(message)
    {

    }
}
