using System;

namespace E.Standard.CMS.Core.Exceptions;

public class CmsCreateInstanceException : Exception
{
    public CmsCreateInstanceException(string message, Exception innerException = null)
        : base(message, innerException)
    {

    }
}
