using System;

namespace E.Standard.Security.Cryptography;

public enum CryptoStrength
{
    AES128 = 1,
    AES192 = 2,
    AES256 = 3
}

public enum CryptoResultStringType
{
    Base64 = 0,
    Hex = 1
}

[Flags]
public enum CustomPasswords
{
    None = 0,
    PortalProxyRequests = 1,
    ApiStoragePassword = 2,
    ApiBridgeUserCryptoPassword = 4,
    ApiAdminQueryPassword = 16
}