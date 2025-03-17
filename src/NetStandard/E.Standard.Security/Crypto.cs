using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace E.Standard.Security
{
    [Obsolete("Use E.Standard.Security.Internal assembly")]
    public class Crypto
    {
        private const int _saltSize = 4, _iterations = 1000;
        private const string defaultPassword = "10CD9814D250440492FC5833A44F77A65B36241C0E044DB6BCF529C219D6F6D04DCDA1C08142413B9B494D04388162BB";

        public Crypto()
        {

        }

        public enum Strength
        {
            AES128 = 1,
            AES192 = 2,
            AES256 = 3
        }

        public enum ResultStringType
        {
            Base64 = 0,
            Hex = 1
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
                using (RijndaelManaged AES = new RijndaelManaged())
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
                    ms.Write(ivInitialBytes, 0, _saltSize);

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
                using (RijndaelManaged AES = new RijndaelManaged())
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
                    var ivInitialBytes = new byte[_saltSize];
                    Buffer.BlockCopy(bytesToBeDecrypted, 0, ivInitialBytes, 0, _saltSize);

                    AES.Key = GetBytes(passwordBytes, AES.KeySize / 8);
                    AES.IV = GetHashedBytes(ivInitialBytes, AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, _saltSize, bytesToBeDecrypted.Length - _saltSize);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            if (useRandomSalt)
            {
                byte[] ret = new byte[decryptedBytes.Length - _saltSize];
                Buffer.BlockCopy(decryptedBytes, _saltSize, ret, 0, ret.Length);
                decryptedBytes = ret;
            }

            return decryptedBytes;
        }

        #endregion

        #region Bytes

        public byte[] EncryptBytes(byte[] bytesToBeEncrypted, string password, Strength strength = Strength.AES128, bool useRandomSalt = true)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            return AES_Encrypt(bytesToBeEncrypted, passwordBytes, GetKeySize(strength), useRandomSalt);
        }

        public byte[] DecryptBytes(byte[] bytesToBeEncrypted, string password, Strength strength = Strength.AES128, bool useRandomSalt = true)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            return AES_Decrypt(bytesToBeEncrypted, passwordBytes, GetKeySize(strength), useRandomSalt);
        }

        #endregion

        #region Text/Strings

        public string EncryptText(string input, string password, Strength strength = Strength.AES128, bool useRandomSalt = true, ResultStringType resultStringType = ResultStringType.Base64)
        {
            if (String.IsNullOrEmpty(input))
                input = "#string.empty#";

            // Get the bytes of the string
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Hash the password with SHA256
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes, GetKeySize(strength), useRandomSalt);

            string result=String.Empty;

            switch (resultStringType)
            {
                case ResultStringType.Base64:
                    result = Convert.ToBase64String(bytesEncrypted);
                    break;
                case ResultStringType.Hex:
                    result = "0x" + string.Concat(bytesEncrypted.Select(b => b.ToString("X2")));
                    break;
            }
            

            return result;
        }

        public string DecryptText(string input, string password, Strength strength = Strength.AES128, bool useRandomSalt = true)
        {
            if (String.IsNullOrEmpty(input))
                return String.Empty;

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
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);

            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes, GetKeySize(strength), useRandomSalt);

            string result = Encoding.UTF8.GetString(bytesDecrypted);
            if (result == "#string.empty#")
                return String.Empty;

            return result;
        }

        public string EncryptTextDefault(string input, ResultStringType resultStringType = ResultStringType.Base64)
        {
            return EncryptText(input, defaultPassword, Strength.AES128, true, resultStringType);
        }

        public string DecryptTextDefault(string input)
        {
            return DecryptText(input, defaultPassword, Strength.AES128, true);
        }

        #region Static Encrypt

        private static byte[] _static_iv = new byte[8] { 11, 127, 102, 78, 48, 67, 12, 96 };

        public string StaticEncrypt(string text, string password, ResultStringType resultStringType = ResultStringType.Base64)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(StaticPassword24(password));
            byte[] inputbuffer = Encoding.UTF8.GetBytes(text);

            SymmetricAlgorithm algorithm = System.Security.Cryptography.TripleDES.Create();
            ICryptoTransform transform = algorithm.CreateEncryptor(passwordBytes, _static_iv);

            List<byte> outputBuffer = new List<byte>(transform.TransformFinalBlock(inputbuffer, 0, inputbuffer.Length));

            string result = String.Empty;
            switch (resultStringType)
            {
                case ResultStringType.Base64:
                    result = Convert.ToBase64String(outputBuffer.ToArray());
                    break;
                case ResultStringType.Hex:
                    result = "0x" + string.Concat(outputBuffer.ToArray().Select(b => b.ToString("X2")));
                    break;
            }

            return result;
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

            SymmetricAlgorithm algorithm = System.Security.Cryptography.TripleDES.Create();
            ICryptoTransform transform = algorithm.CreateDecryptor(passwordBytes, _static_iv);
            byte[] bytesDecrypted = transform.TransformFinalBlock(inputbuffer.ToArray(), 0, inputbuffer.Count);

            string result = Encoding.UTF8.GetString(bytesDecrypted);

            if (result == "#string.emtpy#")
                return String.Empty;

            return result;
        }

        private string StaticPassword24(string password)
        {
            if(String.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Invalid password");
            }
            while(password.Length<24)
            {
                password += password;
            }

            return password.Substring(0, 24);
        }

        public string StaticDefaultEncrypt(string text, ResultStringType resultStringType = ResultStringType.Base64)
        {
            return StaticEncrypt(text, defaultPassword, resultStringType);
        }

        public string StaticDefaultDecrypt(string input)
        {
            return StaticDecrypt(input, defaultPassword);
        }

        #endregion

        #region Config-Settings (static)

        static public string DecryptConfigSettingsValue(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
                return String.Empty;

            if (input.StartsWith("enc:"))
            {
                input = input.Substring(4, input.Length - 4);
                return new Crypto().DecryptTextDefault(input);
            }

            return input;
        }

        static public void DecryptConfigSettingsDocument(XmlDocument xmlDoc)
        {
            if (xmlDoc == null)
                return;

            var allNodes = xmlDoc.SelectNodes("//*");
            foreach (var nodeElement in allNodes)
            {
                if (!(nodeElement is XmlNode))
                    continue;
                
                var node = (XmlNode)nodeElement;
                foreach (var attributeElement in node.Attributes)
                {
                    if (!(attributeElement is XmlAttribute))
                        continue;

                    var attribute = (XmlAttribute)attributeElement;
                    attribute.Value = DecryptConfigSettingsValue(attribute.Value);
                }
            }
        }

        static public string EncryptConfigSettingsValue(string input, ResultStringType resultStringType=ResultStringType.Base64)
        {
            return  "enc:" + new Crypto().EncryptTextDefault(input, resultStringType);
        }

        #endregion

        #region Cookies

        private const string CookiePassword = "Q&+%j@*c+zd*XmWp9QA#^Wbw4LBURsWtf6t!%S*A49F?abtFZ+DJq6=xFKCHr*rGSy@%26byvypMnb3uH@3!ReBAv%jq+QM2S_yAQ%mG-swRH?z77!JzXs58-*%b#9Jg";

        static public string EncryptCookieValue(string value)
        {
            return new Crypto().EncryptText(value, CookiePassword, Strength.AES256, true);
        }

        static public string DecryptCookieValue(string value)
        {
            if(String.IsNullOrWhiteSpace(value))
                return value;

            return new Crypto().DecryptText(value, CookiePassword, Strength.AES256, true);
        }

        #endregion

        #endregion

        #region Hash

        public string Hash64_SHA1(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            passwordBytes = SHA1.Create().ComputeHash(passwordBytes);
            return Convert.ToBase64String(passwordBytes);
        }

        public string Hash64_SHA1(byte[] passwordBytes)
        {
            passwordBytes = SHA1.Create().ComputeHash(passwordBytes);
            return Convert.ToBase64String(passwordBytes);
        }

        public string Hash64_SHA256(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
            return Convert.ToBase64String(passwordBytes);
        }

        public string Hash64_SHA256(byte[] passwordBytes)
        {
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
            return Convert.ToBase64String(passwordBytes);
        }

        public string Hash64_SHA512(string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            passwordBytes = SHA512.Create().ComputeHash(passwordBytes);
            return Convert.ToBase64String(passwordBytes);
        }

        public string Hash64_SHA512(byte[] passwordBytes)
        {
            passwordBytes = SHA512.Create().ComputeHash(passwordBytes);
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
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
            return "0x" + string.Concat(passwordBytes.ToArray().Select(b => b.ToString("X2")));
        }

        public string HashHex(string password, string salt)
        {
            password = salt + password;
            return HashHex(password);
        }

        public bool VerifyPassword(string cleanPassword, string hash)
        {
            if (Hash64(cleanPassword) == hash)
                return true;

            // Hier könnten noch weiter Methoden getestet werden: zB Hash & Salt

            return false;
        }

        // Never Change this!!
        public string CreateConvergentPseudoPasswordFromInputText(string text)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(text).Reverse().ToArray();
            passwordBytes = SHA256.Create().ComputeHash(passwordBytes).Reverse().ToArray();
            return Convert.ToBase64String(passwordBytes);
        }

        public string CreateRandomSecret(int length, string allowedCharacters="0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-_")
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

        #region General

        public string Base64ToHex(string base64)
        {
            if (base64.StartsWith("0x") && IsHexString(base64))
                return base64;

            byte[] bytes = Convert.FromBase64String(base64);

            return "0x" + string.Concat(bytes.Select(b => b.ToString("X2")));
        }

        // Never change this method!!
        // Otherwise update all krako and webGIS Instances and dependencies
        public string ToPasswordString(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            s = new string(charArray) + s;

            s = Hash64(s);

            return s;
        }

        #endregion

        #region Guid

        public string GuidToBase64(Guid guid)
        {
            return Convert.ToBase64String(guid.ToByteArray()).Replace("/", "-").Replace("+", "_").Replace("=", "");
        }

        public Guid Base64ToGuid(string base64)
        {
            Guid guid = default(Guid);
            base64 = base64.Replace("-", "/").Replace("_", "+") + "==";

            try
            {
                guid = new Guid(Convert.FromBase64String(base64));
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid input", ex);
            }

            return guid;
        }

        #endregion

        #region Helper

        public byte[] GetRandomBytes()
        {
            byte[] ba = new byte[_saltSize];
            RNGCryptoServiceProvider.Create().GetBytes(ba);
            return ba;
        }

        public byte[] GetBytes(byte[] initialBytes, int size)
        {
            var ret = new byte[size];
            Buffer.BlockCopy(initialBytes, 0, ret, 0, Math.Min(initialBytes.Length, ret.Length));

            return ret;
        }

        private static byte[] _g1 = new Guid("5c9e6cb9-9574-49fd-ab5d-6a7f42a46c6c").ToByteArray();
        public byte[] GetHashedBytes(byte[] initialBytes, int size)
        {
            var hash = SHA256.Create().ComputeHash(initialBytes);

            var ret = new byte[size];
            Buffer.BlockCopy(hash, 0, ret, 0, Math.Min(hash.Length, ret.Length));

            byte[] saltBytes = new byte[] { 176, 223, 23, 125, 64, 98, 177, 214 };
            var key = new Rfc2898DeriveBytes(hash, _g1, 10); // 10 is enough for this...
            ret = key.GetBytes(size);

            return ret;
        }

        private int GetKeySize(Strength strength)
        {
            switch (strength)
            {
                case Strength.AES128:
                    return 128;
                case Strength.AES192:
                    return 192;
                case Strength.AES256:
                    return 256;
            }

            return 128;
        }

        private byte[] StringToByteArray(String hex)
        {
            if (hex.StartsWith("0x"))
                hex = hex.Substring(2, hex.Length - 2);

            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private bool IsHexString(string hex)
        {
            if (hex.StartsWith("0x"))
                hex = hex.Substring(2, hex.Length - 2);

            bool isHex;
            foreach (var c in hex)
            {
                isHex = ((c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F'));

                if (!isHex)
                    return false;
            }
            return true;
        }

        #endregion

    }
}
