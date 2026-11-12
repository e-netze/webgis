using System;

namespace Api.Core.AppCode.Exceptions;

/// <summary>
/// Expected subscriber login failure. <see cref="ErrorKey"/> is a l10n key (subscribers.login.error.*)
/// shown to the user; details stay in the log.
/// </summary>
public class SubscriberLoginException : Exception
{
    public SubscriberLoginException(string errorKey, Exception innerException = null)
        : base($"Subscriber login failed: {errorKey}", innerException)
    {
        ErrorKey = errorKey;
    }

    public string ErrorKey { get; }
}
