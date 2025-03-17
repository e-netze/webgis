using System;

namespace E.Standard.CMS.Core;

public class RefreshConfirmException : Exception
{
    public RefreshConfirmException(string msg)
        : base(msg)
    {

    }
}
