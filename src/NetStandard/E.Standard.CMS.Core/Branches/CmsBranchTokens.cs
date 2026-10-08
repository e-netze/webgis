using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace E.Standard.CMS.Core.Branches;

/// <summary>
/// Encrypted cms branch tokens: "enc:{hex}"
/// The api (hmac_br) only accepts these tokens, never clear branch names. Tokens are created by the portal
/// (shared default crypto key) for map authors (no expiration) and for temporary branch links.
/// </summary>
static public class CmsBranchTokens
{
    public const string Prefix = "enc:";

    private const string PayloadPrefix = "cms-branch";

    // the api accepts expired tokens for some hours, so that open map sessions do not break.
    // the portal checks the expiration strictly when a map is opened with a branch link
    static public readonly TimeSpan ApiExpirationTolerance = TimeSpan.FromHours(12);

    static public bool IsToken(string value)
        => value != null && value.StartsWith(Prefix, StringComparison.Ordinal) && value.Length > Prefix.Length;

    static public string Create(ICryptoService crypto, string encodedBranch, DateTime? expiresUtc = null)
    {
        if (!CmsBranches.IsValidEncoded(encodedBranch))
        {
            throw new ArgumentException($"Invalid branch: {encodedBranch}");
        }

        long expires = expiresUtc.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(expiresUtc.Value.ToUniversalTime(), DateTimeKind.Utc)).ToUnixTimeSeconds()
            : 0;

        var payload = $"{PayloadPrefix}|{encodedBranch}|{expires}";
        var hex = crypto.EncryptTextDefault($"{payload}|{Checksum(payload)}", CryptoResultStringType.Hex);
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex.Substring(2);
        }

        return Prefix + hex.ToLowerInvariant();
    }

    /// <summary>
    /// Decrypts a token. Returns false, if the token is not valid (format, crypto key, branch name).
    /// The expiration is not checked here (see IsExpired)
    /// </summary>
    static public bool TryRead(ICryptoService crypto, string token, out string encodedBranch, out DateTime? expiresUtc)
    {
        encodedBranch = null;
        expiresUtc = null;

        if (crypto == null || !IsToken(token))
        {
            return false;
        }

        var hex = token.Substring(Prefix.Length);
        if (hex.Length % 2 != 0 || !hex.All(Uri.IsHexDigit))
        {
            return false;
        }

        string payload;
        try
        {
            payload = crypto.DecryptTextDefault($"0x{hex}");
        }
        catch
        {
            return false;
        }

        // the checksum detects manipulated tokens (aes-cbc without mac => bit flipping would corrupt at least one block)
        var parts = payload?.Split('|');
        if (parts == null || parts.Length != 4 ||
            parts[0] != PayloadPrefix ||
            parts[3] != Checksum($"{parts[0]}|{parts[1]}|{parts[2]}") ||
            !CmsBranches.IsValidEncoded(parts[1]) ||
            !long.TryParse(parts[2], out long expires) || expires < 0)
        {
            return false;
        }

        encodedBranch = parts[1];
        expiresUtc = expires == 0
            ? null
            : DateTimeOffset.FromUnixTimeSeconds(expires).UtcDateTime;

        return true;
    }

    static private string Checksum(string payload)
    {
        using (var sha256 = SHA256.Create())
        {
            return String.Concat(sha256.ComputeHash(Encoding.UTF8.GetBytes(payload)).Select(b => b.ToString("x2")));
        }
    }

    static public bool IsExpired(DateTime? expiresUtc, TimeSpan tolerance = default)
        => expiresUtc.HasValue && DateTime.UtcNow > expiresUtc.Value.Add(tolerance);

    /// <summary>
    /// Api: the encoded branch of a valid (not expired) token; otherwise null => main
    /// </summary>
    static public string ResolveEncodedBranch(ICryptoService crypto, string token)
        => TryRead(crypto, token, out string encodedBranch, out DateTime? expiresUtc) &&
           !IsExpired(expiresUtc, ApiExpirationTolerance)
            ? encodedBranch
            : null;
}
