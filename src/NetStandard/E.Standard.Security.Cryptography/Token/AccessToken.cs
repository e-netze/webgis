using E.Standard.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Token.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace E.Standard.Security.Cryptography.Token;

public class AccessToken
{
    private readonly ICryptoService _cryptoService;

    public AccessToken(ICryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    public string Create(Header header, Payload payload)
    {
        string headerJson = JSerializer.Serialize(header);
        string payloadJson = JSerializer.Serialize(payload);

        string headerBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerJson));
        string payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        string unsignedToken = $"{headerBase64}.{payloadBase64}";

        var hashBytesHeader = SHA512.Create().ComputeHash(
            Encoding.UTF8.GetBytes(_cryptoService.EncryptTextDefault(headerBase64, CryptoResultStringType.Hex)));
        var hashBytesPayload = SHA512.Create().ComputeHash(
            Encoding.UTF8.GetBytes(_cryptoService.EncryptTextDefault(payloadBase64, CryptoResultStringType.Hex)));

        return $"{unsignedToken}.{Convert.ToBase64String(Combine(hashBytesHeader, hashBytesPayload))}";
    }

    #region Helper

    public static byte[] Combine(byte[] array1, byte[] array2)
    {
        byte[] ret = new byte[array1.Length + array2.Length];
        Buffer.BlockCopy(array1, 0, ret, 0, array1.Length);
        Buffer.BlockCopy(array2, 0, ret, array1.Length, array2.Length);
        return ret;
    }

    #endregion
}
