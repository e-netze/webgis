using System;
using System.Security.Cryptography;

using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;

namespace E.Standard.Security.Captcha;

public class CaptchaResult
{
    public string CaptchaCode { get; set; }

    public byte[] CaptchaByteData { get; set; }

    public string CaptchBase64Data
    {
        get
        {
            return Convert.ToBase64String(CaptchaByteData);
        }
    }

    /// <summary>
    /// Encrypted, self-contained verification token: "v2|code|issuedUnixSeconds|nonce".
    /// The nonce allows the caller to mark a captcha as used (one-time use).
    /// </summary>
    public string CaptchaCodeEncrypted(ICryptoService crypto)
    {
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var issued = new DateTimeOffset(Timestamp.ToUniversalTime()).ToUnixTimeSeconds();

        return crypto.EncryptTextDefault($"{TokenVersion}|{this.CaptchaCode}|{issued}|{nonce}", CryptoResultStringType.Base64);
    }

    public DateTime Timestamp { get; set; }

    #region Static Members

    private const string TokenVersion = "v2";

    /// <summary>
    /// Verifies the user input against the encrypted token and returns the token nonce.
    /// Throws if the input is missing/wrong, the token is invalid or expired.
    /// </summary>
    static public string VerifyCaptchaCode(ICryptoService crypto, string code, string encryptedCode, TimeSpan lifetime)
    {
        if (String.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Captcha code required");
        }

        if (String.IsNullOrWhiteSpace(encryptedCode))
        {
            throw new ArgumentException("Captcha verification code required");
        }

        string[] parts;
        try
        {
            parts = crypto.DecryptTextDefault(encryptedCode).Split('|');
        }
        catch (Exception ex)
        {
            throw new Exception("Captcha verification code invalid", ex);
        }

        if (parts.Length != 4 || parts[0] != TokenVersion || !long.TryParse(parts[2], out long issued))
        {
            throw new Exception("Captcha verification code invalid");
        }

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issued);
        if (age > lifetime || age < TimeSpan.FromMinutes(-1))
        {
            throw new Exception("Captcha expired");
        }

        if (!String.Equals(parts[1], code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Captcha input not correct");
        }

        return parts[3];
    }

    #endregion
}
