using System;
using System.Security.Cryptography;
using System.Text;

namespace E.Standard.WebGIS.SubscriberDatabase.Security;

/// <summary>
/// Password hashing for subscribers.
/// Current format: <c>pbkdf2-sha256${iterations}${salt-base64}${hash-base64}</c>.
/// Legacy format (still verified, rehashed on next login): 64 hex chars salt + hex SHA-256(UTF-16(password + salt)).
/// </summary>
internal static class SubscriberPasswordHasher
{
    private const string Pbkdf2Prefix = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int HashSize = 32;

    // OWASP recommendation for PBKDF2-HMAC-SHA256. The iteration count is stored in the hash,
    // so raising this value only affects new hashes; older ones are upgraded via NeedsRehash.
    public const int Iterations = 600_000;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password ?? String.Empty, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return $"{Pbkdf2Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        if (String.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        password ??= String.Empty;

        try
        {
            if (storedHash.StartsWith(Pbkdf2Prefix + "$", StringComparison.Ordinal))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4 || !int.TryParse(parts[1], out int iterations) || iterations <= 0)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }

            return VerifyLegacy(password, storedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static bool NeedsRehash(string storedHash)
    {
        if (String.IsNullOrEmpty(storedHash) || !storedHash.StartsWith(Pbkdf2Prefix + "$", StringComparison.Ordinal))
        {
            return true;
        }

        var parts = storedHash.Split('$');
        return parts.Length != 4 || !int.TryParse(parts[1], out int iterations) || iterations < Iterations;
    }

    private static bool VerifyLegacy(string password, string storedHash)
    {
        if (storedHash.Length != 128)
        {
            return false;
        }

        string salt = storedHash.Substring(0, 64);
        byte[] actual = SHA256.HashData(Encoding.Unicode.GetBytes(password + salt));
        byte[] expected = Convert.FromHexString(storedHash.Substring(64));

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
