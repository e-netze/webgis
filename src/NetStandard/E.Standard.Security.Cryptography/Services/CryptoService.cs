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

    //public string StaticEncrypt_3Des(string text, string password, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    //{
    //    try
    //    {
    //        return Impl.StaticEncrypt_3Des(text, password, resultStringType);
    //    }
    //    catch (Exception ex) { throw new CryptographyException(ex); }
    //}

    //public string StaticDecrypt_3Des(string input, string password)
    //{
    //    try
    //    {
    //        return Impl.StaticDecrypt_3Des(input, password);
    //    }
    //    catch (Exception ex) { throw new CryptographyException(ex); }
    //}

    public string StaticDefaultEncrypt_3Des(string text, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        try
        {
#pragma warning disable 0618
            // allowed here, because this methode is itself already marked as obsolte (in ICryptoService interface)
            return Impl.StaticDefaultEncrypt_3Des(text, resultStringType);
#pragma warning restore 0618
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticDefaultDecrypt_3Des(string input)
    {
        try
        {
#pragma warning disable 0618
            // allowed here, because this methode is itself already marked as obsolte (in ICryptoService interface)
            return Impl.StaticDefaultDecrypt_3Des(input);
#pragma warning restore 0618
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticEncrypt_Aes(string text, string password, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        try
        {
            return Impl.StaticEncrypt_Aes(text, password, resultStringType);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticDecrypt_Aes(string input, string password)
    {
        try
        {
            return Impl.StaticDecrypt_Aes(input, password);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticDefaultEncrypt_Aes(string text, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        try
        {
            return Impl.StaticDefaultEncrypt_Aes(text, resultStringType);
        }
        catch (Exception ex) { throw new CryptographyException(ex); }
    }

    public string StaticDefaultDecrypt_Aes(string input)
    {
        try
        {
            return Impl.StaticDefaultDecrypt_Aes(input);
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
