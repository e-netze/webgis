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

    string StaticEncrypt(string text, string password, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);
    string StaticDecrypt(string input, string password);

    string StaticDefaultEncrypt(string text, CryptoResultStringType resultStringType = CryptoResultStringType.Base64);
    string StaticDefaultDecrypt(string input);

    string EncryptCookieValue(string value);

    string DecryptCookieValue(string value);

    bool VerifyCustomPassword(int customPasswordIndex, string passwordCanditate);
    string GetCustomPassword(int customPasswordIndex);
}
