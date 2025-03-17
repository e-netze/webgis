using E.Standard.Security.Cryptography.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace E.Standard.Security.Cryptography;

public partial class CryptoImpl
{
    private readonly CryptoServiceOptions _options;

    public CryptoImpl(CryptoServiceOptions options)
    {
        _options = options;
    }

    #region AES Base

    private byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes, int keySize = 128, bool useRandomSalt = true)
    {
        byte[] encryptedBytes = null;

        if (useRandomSalt)
        {
            // Add Random Salt in front -> two ident objects will produce differnt results
            // Remove the Bytes after decryption
            byte[] randomSalt = GetRandomBytes();
            byte[] bytesToEncrpytWidhSalt = new byte[randomSalt.Length + bytesToBeEncrypted.Length];
            Buffer.BlockCopy(randomSalt, 0, bytesToEncrpytWidhSalt, 0, randomSalt.Length);
            Buffer.BlockCopy(bytesToBeEncrypted, 0, bytesToEncrpytWidhSalt, randomSalt.Length, bytesToBeEncrypted.Length);

            bytesToBeEncrypted = bytesToEncrpytWidhSalt;
        }

        using (MemoryStream ms = new MemoryStream())
        {
            using (var AES = Aes.Create())
            {
                AES.KeySize = keySize;
                AES.BlockSize = 128;

                /*
                // Set your salt here, change it to meet your flavor:
                // The salt bytes must be at least 8 bytes.
                byte[] saltBytes = new byte[] { 176, 223, 23, 125, 64, 98, 177, 214 };
                  
                var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, _iterations);
                AES.Key = key.GetBytes(AES.KeySize / 8);
                AES.IV = key.GetBytes(AES.BlockSize / 8);
                 * */

                // Faster (store 4 bytes to generating IV...)
                byte[] ivInitialBytes = GetRandomBytes();
                ms.Write(ivInitialBytes, 0, _options.Saltsize);

                AES.Key = GetBytes(passwordBytes, AES.KeySize / 8);
                AES.IV = GetHashedBytes(ivInitialBytes, AES.BlockSize / 8);

                AES.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                    cs.Close();
                }
                encryptedBytes = ms.ToArray();
            }
        }

        return encryptedBytes;
    }

    private byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes, int keySize = 128, bool useRandomSalt = true)
    {
        byte[] decryptedBytes = null;

        using (MemoryStream ms = new MemoryStream())
        {
            using (var AES = Aes.Create())
            {
                AES.KeySize = keySize;
                AES.BlockSize = 128;

                /*
                // Set your salt here, change it to meet your flavor:
                // The salt bytes must be at least 8 bytes.
                byte[] saltBytes = new byte[] { 176, 223, 23, 125, 64, 98, 177, 214 };
                 *
                var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, _iterations);
                AES.Key = key.GetBytes(AES.KeySize / 8);
                AES.IV = key.GetBytes(AES.BlockSize / 8);
                */

                // Faster get bytes for IV from 
                var ivInitialBytes = new byte[_options.Saltsize];
                Buffer.BlockCopy(bytesToBeDecrypted, 0, ivInitialBytes, 0, _options.Saltsize);

                AES.Key = GetBytes(passwordBytes, AES.KeySize / 8);
                AES.IV = GetHashedBytes(ivInitialBytes, AES.BlockSize / 8);

                AES.Mode = CipherMode.CBC;

                using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(bytesToBeDecrypted, _options.Saltsize, bytesToBeDecrypted.Length - _options.Saltsize);
                    cs.Close();
                }
                decryptedBytes = ms.ToArray();
            }
        }

        if (useRandomSalt)
        {
            byte[] ret = new byte[decryptedBytes.Length - _options.Saltsize];
            Buffer.BlockCopy(decryptedBytes, _options.Saltsize, ret, 0, ret.Length);
            decryptedBytes = ret;
        }

        return decryptedBytes;
    }

    #endregion

    #region Bytes

    public byte[] EncryptBytes(byte[] bytesToBeEncrypted, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        // Hash the password with SHA256
        using (var algorithm = SHA256.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return AES_Encrypt(bytesToBeEncrypted, passwordBytes, GetKeySize(strength), useRandomSalt);
    }

    public byte[] DecryptBytes(byte[] bytesToBeEncrypted, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        using (var algorithm = SHA256.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return AES_Decrypt(bytesToBeEncrypted, passwordBytes, GetKeySize(strength), useRandomSalt);
    }

    #endregion

    #region Text/Strings

    public string EncryptText(string input, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        if (String.IsNullOrEmpty(input))
        {
            input = "#string.empty#";
        }

        // Get the bytes of the string
        byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        // Hash the password with SHA256
        using (var algorithm = SHA256.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes, GetKeySize(strength), useRandomSalt);

        string result = String.Empty;

        switch (resultStringType)
        {
            case CryptoResultStringType.Base64:
                result = Convert.ToBase64String(bytesEncrypted);
                break;
            case CryptoResultStringType.Hex:
                result = $"0x{string.Concat(bytesEncrypted.Select(b => b.ToString("X2")))}";
                break;
        }


        return result;
    }

    public string DecryptText(string input, string password, CryptoStrength strength = CryptoStrength.AES128, bool useRandomSalt = true)
    {
        if (String.IsNullOrEmpty(input))
        {
            return String.Empty;
        }

        // Get the bytes of the string
        byte[] bytesToBeDecrypted = null;
        if (input.StartsWith("0x") && IsHexString(input))
        {
            bytesToBeDecrypted = StringToByteArray(input);
        }
        else
        {
            bytesToBeDecrypted = Convert.FromBase64String(input);
        }
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        using (var algorithm = SHA256.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes, GetKeySize(strength), useRandomSalt);

        string result = Encoding.UTF8.GetString(bytesDecrypted);
        if (result == "#string.empty#")
        {
            return String.Empty;
        }

        return result;
    }

    public string EncryptTextDefault(string input, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        return EncryptText(input, _options.DefaultPassword, CryptoStrength.AES128, true, resultStringType);
    }

    public string DecryptTextDefault(string input)
    {
        return DecryptText(input, _options.DefaultPassword, CryptoStrength.AES128, true);
    }

    #region Static Encrypt

    private static byte[] _static_iv = new byte[8] { 11, 127, 102, 78, 48, 67, 12, 96 };

    public string StaticEncrypt(string text, string password, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(StaticPassword24(password));
        byte[] inputbuffer = Encoding.UTF8.GetBytes(text);

        using (SymmetricAlgorithm algorithm = System.Security.Cryptography.TripleDES.Create())
        using (ICryptoTransform transform = algorithm.CreateEncryptor(passwordBytes, _static_iv))
        {

            List<byte> outputBuffer = new List<byte>(transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length));

            string result = String.Empty;
            switch (resultStringType)
            {
                case CryptoResultStringType.Base64:
                    result = Convert.ToBase64String(outputBuffer.ToArray());
                    break;
                case CryptoResultStringType.Hex:
                    result = $"0x{string.Concat(outputBuffer.ToArray().Select(b => b.ToString("X2")))}";
                    break;
            }

            return result;
        }
    }

    public string StaticDecrypt(string input, string password)
    {
        List<byte> inputbuffer = new List<byte>();

        if (input.StartsWith("0x") && IsHexString(input))
        {
            inputbuffer.AddRange(StringToByteArray(input));
        }
        else
        {
            inputbuffer.AddRange(Convert.FromBase64String(input));
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(StaticPassword24(password));

        using (SymmetricAlgorithm algorithm = System.Security.Cryptography.TripleDES.Create())
        using (ICryptoTransform transform = algorithm.CreateDecryptor(passwordBytes, _static_iv))
        {

            byte[] bytesDecrypted = transform.TransformFinalBlock(inputbuffer.ToArray(), 0, inputbuffer.Count);

            string result = Encoding.UTF8.GetString(bytesDecrypted);

            if (result == "#string.emtpy#")
            {
                return String.Empty;
            }

            return result;
        }
    }

    private string StaticPassword24(string password)
    {
        if (String.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Invalid password");
        }
        while (password.Length < 24)
        {
            password += password;
        }

        return password.Substring(0, 24);
    }

    public string StaticDefaultEncrypt(string text, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        return StaticEncrypt(text, _options.DefaultPassword, resultStringType);
    }

    public string StaticDefaultDecrypt(string input)
    {
        return StaticDecrypt(input, _options.DefaultPassword);
    }

    #endregion

    #region Config-Settings (static)

    public string DecryptConfigSettingsValue(string input)
    {
        if (String.IsNullOrWhiteSpace(input))
        {
            return String.Empty;
        }

        if (input.StartsWith("enc:"))
        {
            input = input.Substring(4, input.Length - 4);
            return DecryptTextDefault(input);
        }

        return input;
    }

    public void DecryptConfigSettingsDocument(XmlDocument xmlDoc)
    {
        if (xmlDoc == null)
        {
            return;
        }

        var allNodes = xmlDoc.SelectNodes("//*");
        foreach (var nodeElement in allNodes)
        {
            if (!(nodeElement is XmlNode))
            {
                continue;
            }

            var node = (XmlNode)nodeElement;
            foreach (var attributeElement in node.Attributes)
            {
                if (!(attributeElement is XmlAttribute))
                {
                    continue;
                }

                var attribute = (XmlAttribute)attributeElement;
                attribute.Value = DecryptConfigSettingsValue(attribute.Value);
            }
        }
    }

    public string EncryptConfigSettingsValue(string input, CryptoResultStringType resultStringType = CryptoResultStringType.Base64)
    {
        return "enc:" + EncryptTextDefault(input, resultStringType);
    }

    #endregion

    #region Cookies

    public string EncryptCookieValue(string value)
    {
        return EncryptText(value, _options.CookiePassword, CryptoStrength.AES256, true);
    }

    public string DecryptCookieValue(string value)
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return DecryptText(value, _options.CookiePassword, CryptoStrength.AES256, true);
    }

    #endregion

    #endregion

    #region Hash

    public string Hash64_SHA1(string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        using (var algorithm = SHA1.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return Convert.ToBase64String(passwordBytes);
    }

    public string Hash64_SHA1(byte[] passwordBytes)
    {
        using (var algorithm = SHA1.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return Convert.ToBase64String(passwordBytes);
    }

    public string Hash64_SHA256(string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        using (var algorithm = SHA256.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return Convert.ToBase64String(passwordBytes);
    }

    public string Hash64_SHA256(byte[] passwordBytes)
    {
        using (var algorithm = SHA256.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return Convert.ToBase64String(passwordBytes);
    }

    public string Hash64_SHA512(string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        using (var algorithm = SHA512.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return Convert.ToBase64String(passwordBytes);
    }

    public string Hash64_SHA512(byte[] passwordBytes)
    {
        using (var algorithm = SHA512.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return Convert.ToBase64String(passwordBytes);
    }

    public string Hash64(string password)
    {
        // default: never change!
        return Hash64_SHA256(password);
    }

    public string Hash64(string password, string salt)
    {
        password = salt + password;
        return Hash64(password);
    }

    public string HashHex(string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        using (var algorithm = SHA256.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes);
        }

        return $"0x{string.Concat(passwordBytes.ToArray().Select(b => b.ToString("X2")))}";
    }

    public string HashHex(string password, string salt)
    {
        password = salt + password;
        return HashHex(password);
    }

    public bool VerifyPassword(string cleanPassword, string hash)
    {
        if (Hash64(cleanPassword) == hash)
        {
            return true;
        }

        // Hier könnten noch weiter Methoden getestet werden: zB Hash & Salt

        return false;
    }

    // Never Change this!!
    public string CreateConvergentPseudoPasswordFromInputText(string text)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(text).Reverse().ToArray();

        using (var algorithm = SHA256.Create())
        {
            passwordBytes = algorithm.ComputeHash(passwordBytes).Reverse().ToArray();
        }

        return Convert.ToBase64String(passwordBytes);
    }

    public string CreateRandomSecret(int length, string allowedCharacters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_")
    {
        var random = new Random();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < length; i++)
        {
            sb.Append(allowedCharacters[random.Next(allowedCharacters.Length)]);
        }
        return sb.ToString();
    }

    #endregion

    #region Helper

    public byte[] GetRandomBytes()
    {
        byte[] ba = RandomNumberGenerator.GetBytes(_options.Saltsize);
        return ba;
    }

    public byte[] GetBytes(byte[] initialBytes, int size)
    {
        var ret = new byte[size];
        Buffer.BlockCopy(initialBytes, 0, ret, 0, Math.Min(initialBytes.Length, ret.Length));

        return ret;
    }

    public byte[] GetHashedBytes(byte[] initialBytes, int size)
    {
        byte[] hash;
        using (var algorithm = SHA256.Create())
        {
            hash = algorithm.ComputeHash(initialBytes);
        }

        var ret = new byte[size];
        Buffer.BlockCopy(hash, 0, ret, 0, Math.Min(hash.Length, ret.Length));

        byte[] saltBytes = new byte[] { 176, 223, 23, 125, 64, 98, 177, 214 };
        var key = new Rfc2898DeriveBytes(
            hash,
            _options.HashBytesSalt,
            10, // 10 is enough for this...
            hashAlgorithm: HashAlgorithmName.SHA1);
        ret = key.GetBytes(size);

        return ret;
    }

    private int GetKeySize(CryptoStrength strength)
    {
        switch (strength)
        {
            case CryptoStrength.AES128:
                return 128;
            case CryptoStrength.AES192:
                return 192;
            case CryptoStrength.AES256:
                return 256;
        }

        return 128;
    }

    private byte[] StringToByteArray(String hex)
    {
        if (hex.StartsWith("0x"))
        {
            hex = hex.Substring(2, hex.Length - 2);
        }

        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }

    private bool IsHexString(string hex)
    {
        if (hex.StartsWith("0x"))
        {
            hex = hex.Substring(2, hex.Length - 2);
        }

        bool isHex;
        foreach (var c in hex)
        {
            isHex = ((c >= '0' && c <= '9') ||
                     (c >= 'a' && c <= 'f') ||
                     (c >= 'A' && c <= 'F'));

            if (!isHex)
            {
                return false;
            }
        }
        return true;
    }

    #endregion

    #region Static Members

    public static string GetRandomAlphanumericString(int length)
    {
        const string alphanumericCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            "0123456789";
        return GetRandomString(length, alphanumericCharacters);
    }

    public static string GetRandomString(int length, IEnumerable<char> characterSet)
    {
        if (length < 0)
        {
            throw new ArgumentException("length must not be negative", "length");
        }

        if (length > int.MaxValue / 8) // 250 million chars ought to be enough for anybody
        {
            throw new ArgumentException("length is too big", "length");
        }

        if (characterSet == null)
        {
            throw new ArgumentNullException("characterSet");
        }

        var characterArray = characterSet.Distinct().ToArray();
        if (characterArray.Length == 0)
        {
            throw new ArgumentException("characterSet must not be empty", "characterSet");
        }

        var bytes = RandomNumberGenerator.GetBytes(length * 8);
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            ulong value = BitConverter.ToUInt64(bytes, i * 8);
            result[i] = characterArray[value % (uint)characterArray.Length];
        }
        return new string(result);
    }

    public static byte[] GetRandomBytes(int length)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        return bytes;
    }

    //
    //  _defaultPassword is set in the Partial Class in CryptoImpl.HiddenSecrets.cs
    //  If its not compiling:
    //  Copy CryptoImpl.HiddenSecrets__.cs and rename it to CryptoImpl.HiddenScrets.cs. This file schould be ignored by GIT (.gitignore)
    //  Uncomment Code in CryptoImpl.HiddenSecrets.cs und set _defaultPassword for your development environment
    //

    // only use this methods for files that stay on the server!!
    public static string PseudoEncryptString(string input, string password = null)
    {
        var impl = new CryptoImpl(new CryptoServiceOptions());

        return $"enc::{impl.EncryptText(input, password ?? GetStaticPseudoKey(), CryptoStrength.AES256, false)}";
    }

    public static string PseudoDecryptString(string input, string password = null)
    {
        if (input.StartsWith("enc::"))
        {
            var impl = new CryptoImpl(new CryptoServiceOptions());

            return impl.DecryptText(input.Substring("enc::".Length), password ?? GetStaticPseudoKey(), CryptoStrength.AES256, false);
        }

        return input;
    }

    #region Static Pseudo Key

    // generate e pseudo Password for the server side encryption
    private static string __static_defaultKey = "";

    private static string GetStaticPseudoKey()
    {
        if (string.IsNullOrEmpty(__static_defaultKey))
        {
            var ob = new Obsucured();
            var vault = new Vault.KeyVault(ob.GetBase64());

            __static_defaultKey = vault.DecryptKey(ob.Id, ob.GetGuid());
        }

        return __static_defaultKey;
    }

    private class Obsucured
    {
        private byte[] k1 = new byte[]
        {
            52, 126, 107, 50,
            119, 93, 41, 50, 43, 90, 51, 94,
            117, 46, 56, 54, 55, 70, 111, 75, 62, 51, 83,
            78, 102, 56, 55, 88, 45, 125, 61, 68
        };

        private byte[] k2 = new byte[]
        {
            77,235,65,159,65,141,
            65,69,146,24,67,
            153,173,39,
            150,168
        };

        internal string Id => "WebGIS";
        internal string GetBase64() => Convert.ToBase64String(k1);
        internal Guid GetGuid() => new Guid(k2);
    }

    #endregion

    #endregion Static Members
}
