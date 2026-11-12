#nullable enable

using System;
using System.Security.Cryptography;
using System.Text;

namespace Api.Core.AppCode.Services.Authentication.Mfa;

/// <summary>
/// Time-based one-time passwords (RFC 6238, HMAC-SHA1, 6 digits, 30 seconds),
/// compatible with common authenticator apps (Microsoft/Google Authenticator, ...).
/// </summary>
public class TotpService
{
    public const int Digits = 6;
    public const int StepSeconds = 30;
    private const int SecretBytes = 20;
    private const int AllowedClockDriftSteps = 1;

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecret() => ToBase32(RandomNumberGenerator.GetBytes(SecretBytes));

    public string GetOtpAuthUri(string issuer, string accountName, string base32Secret)
    {
        string label = Uri.EscapeDataString($"{issuer}:{accountName}");

        return $"otpauth://totp/{label}?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
    }

    /// <summary>
    /// Verifies a code within the allowed clock drift. Only time steps after <paramref name="lastUsedTimeStep"/>
    /// are accepted, so a code can't be used twice.
    /// </summary>
    /// <returns>The matching time step or null if the code is invalid.</returns>
    public long? VerifyCode(string base32Secret, string? code, long lastUsedTimeStep, DateTimeOffset? now = null)
    {
        code = NormalizeCode(code);
        if (code == null)
        {
            return null;
        }

        byte[] secret = FromBase32(base32Secret);
        long currentStep = GetTimeStep(now ?? DateTimeOffset.UtcNow);

        long? matchedStep = null;
        for (long step = currentStep - AllowedClockDriftSteps; step <= currentStep + AllowedClockDriftSteps; step++)
        {
            // check all steps (no early exit) => constant time
            bool match = CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(ComputeCode(secret, step)),
                Encoding.ASCII.GetBytes(code));

            if (match && step > lastUsedTimeStep && matchedStep == null)
            {
                matchedStep = step;
            }
        }

        return matchedStep;
    }

    public string ComputeCode(byte[] secret, long timeStep)
    {
        byte[] counter = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        byte[] hash = HMACSHA1.HashData(secret, counter);

        int offset = hash[^1] & 0x0f;
        int binary = ((hash[offset] & 0x7f) << 24)
                   | (hash[offset + 1] << 16)
                   | (hash[offset + 2] << 8)
                   | hash[offset + 3];

        return (binary % 1_000_000).ToString("D6");
    }

    public static long GetTimeStep(DateTimeOffset time) => time.ToUnixTimeSeconds() / StepSeconds;

    private static string? NormalizeCode(string? code)
    {
        code = code?.Replace(" ", "").Replace("-", "").Trim();

        if (code == null || code.Length != Digits)
        {
            return null;
        }

        foreach (char c in code)
        {
            if (c < '0' || c > '9')
            {
                return null;
            }
        }

        return code;
    }

    #region Base32 (RFC 4648, no padding)

    public static string ToBase32(byte[] data)
    {
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;

        foreach (byte b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Base32Alphabet[(buffer >> (bits - 5)) & 0x1f]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            sb.Append(Base32Alphabet[(buffer << (5 - bits)) & 0x1f]);
        }

        return sb.ToString();
    }

    public static byte[] FromBase32(string base32)
    {
        base32 = base32.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant();

        var result = new byte[base32.Length * 5 / 8];
        int buffer = 0, bits = 0, index = 0;

        foreach (char c in base32)
        {
            int value = Base32Alphabet.IndexOf(c);
            if (value < 0)
            {
                throw new FormatException("Invalid base32 character");
            }

            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                result[index++] = (byte)((buffer >> (bits - 8)) & 0xff);
                bits -= 8;
            }
        }

        return result;
    }

    #endregion
}
