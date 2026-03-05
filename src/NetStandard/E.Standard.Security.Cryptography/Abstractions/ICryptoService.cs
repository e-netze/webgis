using System;

namespace E.Standard.Security.Cryptography.Abstractions;

public interface ICryptoService
{
    string EncryptTextDefault(string input, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);

    string DecryptTextDefault(string input);

    string EncryptText(string input, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);
    string EncryptText(string input, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);

    string DecryptText(string input, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true);
    string DecryptText(string input, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true);

    byte[] EncryptBytes(byte[] bytesToBeEncrypted, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true);
    byte[] DecryptBytes(byte[] bytesToBeEncrypted, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true);

    bool VerifyPassword(string cleanPassword, string hash);

    //[Obsolete("Use StaticEncrypt_Aes instead")]
    //string StaticEncrypt_3Des(string text, string password, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);
    //[Obsolete("Use StaticDecrypt_Aes instead")]
    //string StaticDecrypt_3Des(string input, string password);

    [Obsolete("Use StaticDefaultEncrypt_Aes instead")]
    string StaticDefaultEncrypt_3Des(string text, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);
    [Obsolete("Use StaticDefaultDecrypt_Aes instead")]
    string StaticDefaultDecrypt_3Des(string input);

    string StaticEncrypt_Aes(string text, string password, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);
    string StaticDecrypt_Aes(string input, string password);

    string StaticDefaultEncrypt_Aes(string text, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);
    string StaticDefaultDecrypt_Aes(string input);


    string EncryptCookieValue(string value);

    string DecryptCookieValue(string value);

    bool VerifyCustomPassword(int customPasswordIndex, string passwordCanditate);
    string GetCustomPassword(int customPasswordIndex);
}
