using System;
using System.Security.Cryptography;
using System.Text;

namespace E.Standard.WebGIS.CmsSchema;

class Crypto
{
    static public string HashString(string password)
    {
        if (String.IsNullOrEmpty(password))
        {
            return String.Empty;
        }

        using var md5 = MD5.Create();
        byte[] byteValue = UTF8Encoding.UTF8.GetBytes(password);
        byte[] byteHash = md5.ComputeHash(byteValue);
        md5.Clear();

        return Convert.ToBase64String(byteHash);
    }

    static public string GetID()
    {
        //return "i" + Guid.NewGuid().ToString("N");
        return $"i{Standard.CMS.Core.GuidEncoder.Encode(Guid.NewGuid())}";
    }

    static public string GetHashId(string id, string rootPath)
    {
        using (var hashAlgorithm = SHA1.Create())
        {
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(id);
            byte[] hash = hashAlgorithm.ComputeHash(inputBytes);
            string hashString = BitConverter.ToString(hash).Replace("-", "").ToLower(); // Convert.ToBase64String(hash).ToLower();

            string validFilename = hashString
                .Replace("/", "")
                .Replace("+", "")
                .Replace("=", "").Substring(0, 27);

            string hashId = $"i{validFilename}";  // make it compatible with GetID => same length and prefix

            return hashId;
        }
    }
}
