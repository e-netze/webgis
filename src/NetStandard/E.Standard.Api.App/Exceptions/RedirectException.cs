using System;

namespace E.Standard.Api.App.Extensions;

public class RedirectException : Exception
{
    public RedirectException(string redirectUrl)
    {
        this.RedirectUrl = redirectUrl;
    }

    public string RedirectUrl { get; private set; }
}