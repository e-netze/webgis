using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebGIS.Tools.Extensions;

static public class StorageExtensions
{
    static public string MapName2StorageDirectory(this string mapName)
    {
        foreach (var c in new string[] { " ", ":", "?", "!" })
        {
            mapName = mapName.Replace(c, "_");
        }

        return mapName;
    }

    static public string ToValidEncodedName(this string name)
    {
        return $"hex-{ByteArrayToString(Encoding.UTF8.GetBytes(name.Trim()))}";
    }

    static public string FromValidEncodedName(this string encodedName)
    {
        if (!String.IsNullOrEmpty(encodedName))
        {
            if (encodedName.StartsWith("hex-"))
            {
                return Encoding.UTF8.GetString(StringToByteArray(encodedName.Substring("hex-".Length)));
            }
            if (encodedName.StartsWith("base64-"))
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(encodedName.Substring("base64-".Length)));
            }
        }

        return encodedName;
    }

    static public IEnumerable<string> FromValidEncodedNames(this IEnumerable<string> encodedNames, bool ordered = true)
    {
        var result = encodedNames?.Select(s => s.FromValidEncodedName());

        if (ordered)
        {
            return result?.OrderBy(s => s);
        }

        return result;
    }

    #region Helper

    private static string ByteArrayToString(byte[] byteArray)
    {
        return BitConverter.ToString(byteArray).Replace("-", "");
    }

    public static byte[] StringToByteArray(String hex)
    {
        int numberChars = hex.Length;
        byte[] bytes = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }

    #endregion
}
