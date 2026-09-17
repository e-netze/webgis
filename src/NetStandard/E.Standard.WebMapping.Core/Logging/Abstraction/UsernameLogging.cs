#nullable enable

using System;
using System.Security.Cryptography;
using System.Text;

namespace E.Standard.WebMapping.Core.Logging.Abstraction;

/// <summary>
/// Shared implementation of <see cref="UsernameLoggingMode"/> - applies the configured mode to a
/// raw username, so every logger backend (CSV/Microsoft/database) treats "none"/"plaintext"/
/// "hash" identically.
/// </summary>
public static class UsernameLogging
{
    /// <summary>
    /// Returns <paramref name="username"/> transformed per <paramref name="mode"/>: <c>null</c>
    /// for <see cref="UsernameLoggingMode.None"/> (or an empty/whitespace-only input), unchanged
    /// for <see cref="UsernameLoggingMode.PlainText"/>, or its SHA-256 hash (see
    /// <see cref="Hash"/>) for <see cref="UsernameLoggingMode.Hash"/>.
    /// </summary>
    public static string? Apply(UsernameLoggingMode mode, string? username)
    {
        if (String.IsNullOrWhiteSpace(username) || mode == UsernameLoggingMode.None)
        {
            return null;
        }

        return mode == UsernameLoggingMode.Hash ? Hash(username) : username;
    }

    /// <summary>
    /// SHA-256 of the trimmed, invariant-lowercased <paramref name="username"/>, hex-encoded (64
    /// lowercase characters) - not salted by design, so the same user always hashes identically
    /// regardless of casing (needed to recognize "the same user" across log rows); this is a
    /// one-way anonymization/pseudonymization hash, not a password hash.
    /// </summary>
    public static string Hash(string username)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(username.Trim().ToLowerInvariant()));

        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }
}
