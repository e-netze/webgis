namespace E.Standard.Security.Cryptography.Abstractions;

public interface IAppCryptography
{
    string SecurityEncryptString(string input);
    string SecurityEncryptString(string input, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES256);

    string SecurityDecryptString(string encInput);
    string SecurityDecryptString(string encInput, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES256);

    /// <summary>
    /// Like <see cref="SecurityDecryptString(string)"/>, but also accepts legacy (default key AES-CBC) values that may be
    /// persisted (stored urls/ids). Returns null for any invalid input - callers must treat null exactly like "not found",
    /// so the legacy path does not become a padding oracle.
    /// </summary>
    string SecurityDecryptStringOrLegacy(string encInput);

    /// <summary>
    /// Encrypts with the legacy format (default key AES-CBC, hex) - only for values that are persisted and may be
    /// copied between installations (e.g. test -> production storage). Decrypt with <see cref="SecurityDecryptStringOrLegacy"/>.
    /// </summary>
    string SecurityEncryptStringLegacy(string input);
}
