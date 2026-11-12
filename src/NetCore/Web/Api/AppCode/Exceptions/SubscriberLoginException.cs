using System;

namespace Api.Core.AppCode.Exceptions;

/// <summary>
/// Expected subscriber login failure. <see cref="ErrorKey"/> is a l10n key (subscribers.login.error.*)
/// shown to the user; details stay in the log.
/// </summary>
public class SubscriberLoginException : Exception
{
    public SubscriberLoginException(string errorKey, Exception innerException = null)
        : this(errorKey, null, innerException)
    {
    }

    /// <param name="errorArgument">optional value for a {0} placeholder in the localized message (e.g. remaining lockout minutes)</param>
    public SubscriberLoginException(string errorKey, string errorArgument, Exception innerException = null)
        : base($"Subscriber login failed: {errorKey}", innerException)
    {
        ErrorKey = errorKey;
        ErrorArgument = errorArgument;
    }

    public string ErrorKey { get; }

    public string ErrorArgument { get; }
}
