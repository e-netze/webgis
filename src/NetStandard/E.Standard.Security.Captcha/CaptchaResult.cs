using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using System;

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

    public string CaptchaCodeEncrypted(ICryptoService crypto)
    {
        return crypto.EncryptTextDefault(this.CaptchaCode, CryptoResultStringType.Base64);
    }

    public DateTime Timestamp { get; set; }

    #region Static Members

    static public void VerifyCaptchaCode(ICryptoService crypto, string username, string code, string encryptedCode)
    {
        if (String.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Captcha code required");
        }

        if (String.IsNullOrWhiteSpace(encryptedCode))
        {
            throw new ArgumentException("Captcha verification code required");
        }

        if (crypto.DecryptTextDefault(encryptedCode).ToLower() != code.ToLower())
        {
            throw new Exception("Captcha input not correct");
        }
    }

    #endregion
}
