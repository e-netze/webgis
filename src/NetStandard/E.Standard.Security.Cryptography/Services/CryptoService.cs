using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Exceptions;
using Microsoft.Extensions.Options;
using System;

namespace E.Standard.Security.Cryptography.Services;

public class CryptoService : ICryptoService
{
    private readonly CryptoServiceOptions _options;

    public CryptoService(IOptions<CryptoServiceOptions> optionsMonitor)
    {
        _options = optionsMonitor.Value;
    }

    public string DecryptTextDefault(string input)
    {
        try
        {
            return Impl.DecryptTextDefault(input);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string EncryptTextDefault(string input, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        try
        {
            return Impl.EncryptTextDefault(input, resultStringType);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string EncryptText(string input, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        try
        {
            return Impl.EncryptText(input, password, strength, useRandomSalt, resultStringType);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string EncryptText(string input, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        try
        {
            return EncryptText(input, _options.GetCustomPassword(customPasswordIndex), strength, useRandomSalt, resultStringType);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string DecryptText(string input, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true)
    {
        try
        {
            return Impl.DecryptText(input, password, strength, useRandomSalt);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string DecryptText(string input, int customPasswordIndex, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true)
    {
        try
        {
            return DecryptText(input, _options.GetCustomPassword(customPasswordIndex), strength, useRandomSalt);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public bool VerifyPassword(string cleanPassword, string hash)
    {
        try
        {
            return Impl.VerifyPassword(cleanPassword, hash);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticEncrypt(string text, string password, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        try
        {
            return Impl.StaticEncrypt(text, password, resultStringType);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticDecrypt(string input, string password)
    {
        try
        {
            return Impl.StaticDecrypt(input, password);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticDefaultEncrypt(string text, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        try
        {
            return Impl.StaticDefaultEncrypt(text, resultStringType);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticDefaultDecrypt(string input)
    {
        try
        {
            return Impl.StaticDefaultDecrypt(input);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string EncryptCookieValue(string value)
    {
        try
        {
            return Impl.EncryptCookieValue(value);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string DecryptCookieValue(string value)
    {
        try
        {
            return Impl.DecryptCookieValue(value);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public bool VerifyCustomPassword(int customPasswordIndex, string passwordCanditate)
    {
        return !String.IsNullOrWhiteSpace(passwordCanditate) && passwordCanditate == _options.GetCustomPassword(customPasswordIndex);
    }

    public string GetCustomPassword(int customPasswordIndex)
    {
        return _options.GetCustomPassword(customPasswordIndex);
    }

    public byte[] EncryptBytes(byte[] bytesToBeEncrypted, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true)
    {
        try
        {
            return Impl.EncryptBytes(bytesToBeEncrypted, password, strength, useRandomSalt);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public byte[] DecryptBytes(byte[] bytesToBeEncrypted, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true)
    {
        try
        {
            return Impl.DecryptBytes(bytesToBeEncrypted, password, strength, useRandomSalt);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    #region Implementation

    private CryptoImpl _cryptoImpl = null;

    private CryptoImpl Impl
    {
        get
        {
            if (_cryptoImpl == null)
            {
                _cryptoImpl = new CryptoImpl(_options);
            }

            return _cryptoImpl;
        }
    }

    #endregion
}
