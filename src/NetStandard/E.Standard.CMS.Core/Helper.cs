using E.Standard.CMS.Core.Extensions;
using E.Standard.Extensions.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace E.Standard.CMS.Core;

public class Helper
{
    static public bool IsAspNet = false;
    static public object GetRelInstance(XmlNode schemanode,
                                        CmsItemTransistantInjectionServicePack servicePack)
    {
        if (schemanode == null ||
           schemanode.Attributes["assembly"] == null ||
           schemanode.Attributes["instance"] == null)
        {
            return null;
        }

        return GetRelInstance(schemanode.Attributes["assembly"].Value,
                              schemanode.Attributes["instance"].Value,
                              servicePack);
    }
    static public object GetRelInstance(string relAssemblyPath, string typeName, CmsItemTransistantInjectionServicePack servicePack)
    {
        if (IsAspNet == false)
        {
            return GetAbsInstance(
                            Path.Combine(
                                    Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) ?? "",
                                    relAssemblyPath
                            ),
                            typeName,
                            servicePack);
        }
        else
        {
            throw new NotImplementedException();
            //return GetAbsInstance(System.Web.HttpContext.Current.Server.MapPath(@".\bin\" + relAssemblyPath), type);
        }
    }
    static public object GetAbsInstance(string assemblyPath, string typeName, CmsItemTransistantInjectionServicePack servicePack)
    {
        if (String.IsNullOrEmpty(assemblyPath) || String.IsNullOrEmpty(typeName))
        {
            return null;
        }
        //try
        {
            Assembly assembly = File.Exists(assemblyPath)
                ? Assembly.LoadFrom(assemblyPath)
                : typeof(Helper).Assembly;   // single file command line tool?

            var type = assembly.GetType(typeName, false, false);

            return type.CmsCreateInstance(servicePack);
        }
        //catch
        //{
        //    return null;
        //}
    }
    static public string XPathToLowerCase(string attributeName)
    {
        return "translate(@" + attributeName + ", 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')";
    }
    static public string NewLinkName()
    {
        //return "l" + Guid.NewGuid().ToString("N") + ".link";
        return "l" + GuidEncoder.Encode(Guid.NewGuid()) + ".link";
    }

    static public string RegularUrl(string url)
    {
        url = url.Replace("ä", "ae");
        url = url.Replace("ö", "oe");
        url = url.Replace("ü", "ue");
        url = url.Replace("Ä", "AE");
        url = url.Replace("Ö", "OE");
        url = url.Replace("Ü", "UE");
        url = url.Replace("ß", "ss");
        url = url.Replace(" ", "_");

        return url;
    }
    static public string TrimPathRight(string path, int trim)
    {
        string[] parts = path.Replace(@"\", "/").Split('/');
        if (parts.Length <= trim)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < parts.Length - trim; i++)
        {
            if (i > 0)
            {
                sb.Append("/");
            }

            sb.Append(parts[i]);
        }

        return sb.ToString();
    }
    static public string TrimPathLeft(string path, int trim)
    {
        string[] parts = path.Replace(@"\", "/").Split('/');
        if (parts.Length <= trim)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = parts.Length - trim; i < parts.Length; i++)
        {
            if (sb.Length > 0)
            {
                sb.Append("/");
            }

            sb.Append(parts[i]);
        }

        return sb.ToString();
    }

    static public string ArrayToString(string[] array)
    {
        if (array == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        foreach (string item in array)
        {
            if (String.IsNullOrEmpty(item))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append(";");
            }

            sb.Append(item);
        }

        return sb.ToString();
    }
    static public string[] StringToArray(string l)
    {
        if (String.IsNullOrEmpty(l.Trim()))
        {
            return null;
        }

        return l.Split(';');
    }

    static public string[] GetKeyParameters(string commandLine)
    {
        if (String.IsNullOrEmpty(commandLine))
        {
            return null;
        }

        int pos1 = 0, pos2;
        pos1 = commandLine.IndexOf("[");
        string parameters = "";

        while (pos1 != -1)
        {
            pos2 = commandLine.IndexOf("]", pos1);
            if (pos2 == -1)
            {
                break;
            }

            if (parameters != "")
            {
                parameters += ";";
            }

            parameters += commandLine.Substring(pos1 + 1, pos2 - pos1 - 1);
            pos1 = commandLine.IndexOf("[", pos2);
        }
        if (parameters != "")
        {
            return parameters.Split(';');
        }
        else
        {
            return null;
        }
    }

    static public string[] GetKeyParameterFields(string commandLine)
    {
        string[] keys = GetKeyParameters(commandLine);
        return Parameters2FieldNames(keys);
    }

    static public string[] Parameters2FieldNames(string[] names)
    {
        if (names == null)
        {
            return null;
        }

        List<string> fieldNames = new List<string>();
        foreach (string name in names
                                    .Select(n => n.RemovePrefixIfPresent("url-encode:"))
                                    )
        {
            if (name.StartsWith("spatial::"))
            {
                continue;
            }

            if (name.Contains(":"))
            {
                fieldNames.Add(name.Split(':')[0]);
            }
            else if (name.StartsWith("!"))
            {
                fieldNames.Add(name.Substring(1, name.Length - 1));
            }
            else if (name.Contains(","))
            {
                var keys = name.Split(',');
                if (keys.Length <= 3)
                {
                    // Parameter können auch die Form [KG,GNR,|] oder [KG,GNR] haben. Der 3. Parameter ist dann ein Platzhalter und soll nicht übernommen werden.
                    // Aufgelöst werden solche Kunstrukte im Feature Indexer: feature[key]=...

                    for (int i = 0; i < Math.Min(keys.Length, 2); i++)
                    {
                        fieldNames.Add(keys[i].Trim());
                    }
                }
            }
            else
            {
                fieldNames.Add(name);
            }
        }
        return fieldNames.ToArray();
    }

    static public Version GetAssemblyVersion(Type obj)
    {
        if (obj == null)
        {
            return new Version(1, 0, 0, 0);
        }

        return obj.Assembly.GetName().Version;
    }

    static public bool IsValidUrl(string url, out string errMsg)
    {
        if (String.IsNullOrEmpty(url))
        {
            errMsg = "Darf nicht leer sein!";
            return false;
        }
        string invalid = " öäüÖÄÜß!\"\\§$%&/()=?+*#':.,µ@<>|^°{[]}";
        for (int i = 0; i < invalid.Length; i++)
        {
            if (url.Contains(invalid[i].ToString()))
            {
                errMsg = "Ungültiges Zeichen: '" + invalid[i] + "'";
                return false;
            }
        }

        string invalidStarters = "0123456789";
        for (int i = 0; i < invalidStarters.Length; i++)
        {
            if (url.StartsWith(invalidStarters[i].ToString()))
            {
                errMsg = "Darf nicht mit " + invalidStarters[i] + " beginnen.";
                return false;
            }
        }

        errMsg = String.Empty;
        return true;
    }

    static public string ToValidUrl(string name)
    {
        if (String.IsNullOrEmpty(name))
        {
            return "_empty";
        }

        string url = name.ToLower().Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");

        string invalid = " !\"\\§$%&/()=?+*#':.,µ@<>|^°{[]}–";  // – => in not an real minus!
        for (int i = 0; i < invalid.Length; i++)
        {
            url = url.Replace(invalid[i].ToString(), "_");
        }

        string invalidStarters = "0123456789";
        for (int i = 0; i < invalidStarters.Length; i++)
        {
            if (url.StartsWith(invalidStarters[i].ToString()))
            {
                url = "_" + url;
            }
        }

        return url;
    }
}

public class Bit
{
    static public bool Has(int val, int bit)
    {
        return ((val & bit) == bit);
    }
}

public class EnumHelper
{
    static public int FromString(Type e, string name)
    {
        foreach (object x in Enum.GetValues(e))
        {
            if (Enum.GetName(e, x) == name)
            {
                return Convert.ToInt32(x);
            }
        }
        return 0;
    }
}

public static class GuidEncoder
{
    public static string Encode(string guidText)
    {
        Guid guid = new Guid(guidText);
        return Encode(guid);
    }

    public static string Encode(Guid guid)
    {
        //string enc = Convert.ToBase64String(guid.ToByteArray());
        //enc = enc.Replace("/", "_");
        //enc = enc.Replace("+", "-");
        //return enc.Substring(0, 22);

        return ToBase32String(guid.ToByteArray());
    }

    #region Base32 

    private static readonly char[] _digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToLower().ToCharArray();
    private const int _mask = 31;
    private const int _shift = 5;

    private static string ToBase32String(byte[] data)
    {
        return ToBase32String(data, 0, data.Length);
    }

    private static string ToBase32String(byte[] data, int offset, int length)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if ((offset + length) > data.Length)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (length == 0)
        {
            return "";
        }

        // SHIFT is the number of bits per output character, so the length of the
        // output is the length of the input multiplied by 8/SHIFT, rounded up.
        // The computation below will fail, so don't do it.
        if (length >= (1 << 28))
        {
            throw new ArgumentOutOfRangeException(nameof(data));
        }

        var outputLength = (length * 8 + _shift - 1) / _shift;
        var result = new StringBuilder(outputLength);

        var last = offset + length;
        int buffer = data[offset++];
        var bitsLeft = 8;
        while (bitsLeft > 0 || offset < last)
        {
            if (bitsLeft < _shift)
            {
                if (offset < last)
                {
                    buffer <<= 8;
                    buffer |= (data[offset++] & 0xff);
                    bitsLeft += 8;
                }
                else
                {
                    int pad = _shift - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }
            int index = _mask & (buffer >> (bitsLeft - _shift));
            bitsLeft -= _shift;
            result.Append(_digits[index]);
        }

        return result.ToString();
    }

    #endregion
}
