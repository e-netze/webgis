namespace E.Standard.Security.Cryptography.Abstractions;

public interface IAppCryptography
{
    string SecurityEncryptString(string input);

    /// <summary>
    /// Authenticated encryption bound to a dedicated purpose (see <c>CryptoPurposes</c>), so the value can't be
    /// replayed to an endpoint expecting another purpose.
    /// </summary>
    string SecurityEncryptString(string input, string purpose);
    string SecurityEncryptString(string input, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES256);

    string SecurityDecryptString(string encInput);
    string SecurityDecryptString(string encInput, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES256);

    /// <summary>
    /// Decrypts a value encrypted with <see cref="SecurityEncryptString(string, string)"/> for <paramref name="purpose"/>.
    /// Returns null for any invalid input (no legacy fallback).
    /// </summary>
    string SecurityTryDecryptString(string encInput, string purpose);

    /// <summary>
    /// Like <see cref="SecurityDecryptString(string)"/>, but also accepts legacy (default key AES-CBC) values that may be
    /// persisted (stored urls/ids). Returns null for any invalid input - callers must treat null exactly like "not found",
    /// so the legacy path does not become a padding oracle.
    /// </summary>
    string SecurityDecryptStringOrLegacy(string encInput);

    /// <summary>
    /// Like <see cref="SecurityDecryptStringOrLegacy(string)"/>, but the authenticated value must be bound to <paramref name="purpose"/>.
    /// </summary>
    string SecurityDecryptStringOrLegacy(string encInput, string purpose);

    /// <summary>
    /// Encrypts with the legacy format (default key AES-CBC, hex) - only for values that are persisted and may be
    /// copied between installations (e.g. test -> production storage). Decrypt with <see cref="SecurityDecryptStringOrLegacy"/>.
    /// </summary>
    string SecurityEncryptStringLegacy(string input);
}
