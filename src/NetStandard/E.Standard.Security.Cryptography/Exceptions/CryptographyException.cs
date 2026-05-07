using System;

using E.Standard.Extensions.Abstractions;

namespace E.Standard.Security.Cryptography.Exceptions;

public class CryptographyException : Exception, IGenericExceptionMessage
{
    const string GenericErrorMessage = "Generic Cryptographic Excepiton";

    public CryptographyException()
        : base(GenericErrorMessage)
    { }

    public CryptographyException(Exception inner)
        : base(GenericErrorMessage, inner)
    { }

    public string GenericMessage => GenericErrorMessage;
}
