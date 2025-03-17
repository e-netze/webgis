using System;

namespace Portal.Core.AppCode.Exceptions;

public class RedirectException : Exception
{
    public RedirectException(string redirectUrl)
    {
        this.RedirectUrl = redirectUrl;
    }

    public string RedirectUrl { get; private set; }
}
