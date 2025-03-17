namespace E.Standard.Security.Cryptography.Abstractions;

public interface IAppCryptography
{
    string SecurityEncryptString(string input);
    string SecurityEncryptString(string input, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES256);

    string SecurityDecryptString(string encInput);
    string SecurityDecryptString(string encInput, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES256);
}
