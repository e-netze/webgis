using E.Standard.Security.Cryptography.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace E.Standard.Api.App.Extensions;

public static class StringExtension
{
    public static bool IsValidUsername(this string s)
    {
        if (String.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (s.Length < 5 || s.Length > 64)
        {
            return false;
        }

        string pattern = "^[a-z0-9._-]+${5,32}";

        var regex = new Regex(pattern);
        bool ret = regex.IsMatch(s);

        if (ret)
        {
            if (s.StartsWith(".") || s.EndsWith(".") ||
                s.StartsWith("_") || s.EndsWith("_") ||
                s.StartsWith("-") || s.EndsWith("-"))
            {
                return false;
            }

            Guid guid;
            if (Guid.TryParse(s, out guid))  // Username is Guid!!! not allowed
            {
                return false;
            }
        }

        return ret;
    }

    public static bool IsValidEmailAddress(this string s)
    {
        if (String.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        string pattern =
            @"^([0-9a-zA-Z]" + //Start with a digit or alphabate
            @"([\+\-_\.][0-9a-zA-Z]+)*" + // No continues or ending +-_. chars in email
            @")+" +
            @"@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,17})$";
        //string pattern = @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";

        var regex = new Regex(pattern);
        return regex.IsMatch(s);
    }

    public static bool IsValidPassword(this string s)
    {
        if (s == null || s.Length == 0)
        {
            return false;
        }

        if (s.Contains(" "))
        {
            return false;
        }

        if (s.Length < 8)
        {
            return false;
        }

        return true;
    }

    public static bool IsValidUrlId(this string s)
    {
        if (String.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        if (s.Length < 3 || s.Length > 32)
        {
            return false;
        }

        string pattern = "^[a-z0-9-]+${3,32}";

        var regex = new Regex(pattern);
        bool ret = regex.IsMatch(s);

        if (ret)
        {
            if (s.StartsWith(".") || s.EndsWith(".") ||
                s.StartsWith("_") || s.EndsWith("_") ||
                s.StartsWith("-") || s.EndsWith("-"))
            {
                return false;
            }

            Guid guid;
            if (Guid.TryParse(s, out guid))  // UrlId is Guid!!! not allowed
            {
                return false;
            }
        }

        return ret;
    }

    static public string ToValidCmsUrl(this string name)
    {
        if (String.IsNullOrEmpty(name))
        {
            return "_empty";
        }

        string url = name.ToLower().Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");

        string invalid = " !\"\\§$%&/()=?+*#':.,µ@<>|^°{[]}";
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

    public static string ToJavascriptStringArray(this string[] stringArray)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");

        foreach (var val in stringArray)
        {
            if (sb.Length > 1)
            {
                sb.Append(",");
            }

            sb.Append("'");
            sb.Append(val);
            sb.Append("'");
        }

        sb.Append("]");

        return sb.ToString();
    }

    public static string RemoveUrlSchema(this string s)
    {
        if (s.ToLower().StartsWith("http://"))
        {
            return s.Substring(7);
        }

        if (s.ToLower().StartsWith("https://"))
        {
            return s.Substring(8);
        }

        return s;
    }

    public static bool MatchReferer(this string url, string[] templates)
    {
        foreach (var template in templates)
        {
            if (url.MatchReferer(template))
            {
                return true;
            }
        }

        return false;
    }

    public static bool MatchReferer(this string url, string template)
    {
        url = url.RemoveUrlSchema().Split('?')[0].ToLower();

        template = template.Trim().RemoveUrlSchema().Split('?')[0].ToLower();

        string[] urlParts = url.Split('/');
        string[] templateParts = template.Split('/');

        if (urlParts.Length < templateParts.Length)
        {
            return false;
        }

        for (int p = 0; p < templateParts.Length; p++)
        {
            if (p == 0) // Host
            {
                if (urlParts[p] == templateParts[p])
                {
                    continue;
                }

                string[] urlHost = urlParts[p].Split('.');
                string[] templateHost = templateParts[p].Split('.');

                if (urlHost.Length != templateHost.Length)
                {
                    return false;
                }

                for (int h = 0; h < templateHost.Length; h++)
                {
                    if (h < templateHost.Length - 2 && templateHost[h] == "*")  // Die letzten beiden dürfen keine Wildcards sein *.*.steiermark.at ist erlaubt 
                    {
                        continue;
                    }
                    else if (templateHost[h] != urlHost[h])
                    {
                        return false;
                    }
                }
            }
            else
            {
                if (templateParts[p] == "*")
                {
                    continue;
                }

                if (!String.IsNullOrWhiteSpace(templateParts[p]) && urlParts[p] != templateParts[p])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static string EncryptStringProperty(this string str, ICryptoService cryptoService)
    {
        if (str.StartsWith("enc:"))
        {
            return str;
        }

        return "enc:" + cryptoService.EncryptTextDefault(str);
    }

    public static string DecryptStringProperty(this string str, ICryptoService cryptoService)
    {
        if (str.StartsWith("enc:"))
        {
            str = cryptoService.DecryptTextDefault(str.Substring(4));
        }

        return str;
    }

    public static string ToFilename(this string str)
    {
        str = str.Replace(@"\", "/");
        if (str.Contains("/"))
        {
            return str.Substring(str.LastIndexOf("/") + 1);
        }

        return str;
    }

    public static NameValueCollection Clone(this NameValueCollection collection, IEnumerable<string> excludeKeys)
    {
        if (excludeKeys == null)
        {
            return collection;
        }

        var nvc = new NameValueCollection();

        foreach (string k in collection.Keys)
        {
            if (!excludeKeys.Contains(k))
            {
                nvc[k] = collection[k];
            }
        }

        return nvc;
    }

    public static string ToFilterString(this NameValueCollection collection)
    {
        StringBuilder sb = new StringBuilder();

        //sb.Append("?");
        foreach (string k in collection.Keys)
        {
            if (sb.Length > 1)
            {
                sb.Append("&");
            }

            sb.Append(k);
            sb.Append("=");
            sb.Append(System.Web.HttpUtility.UrlDecode(collection[k]));
        }

        return sb.ToString();
    }

    static public string ToProtectedEmail(this string emailAddress)
    {
        if (emailAddress.Contains("@"))
        {
            emailAddress = emailAddress.Split('@')[0].Substring(0, 2).PadRight(emailAddress.Split('@')[0].Length, '*') + "@" + emailAddress.Split('@')[1];
        }

        return emailAddress;
    }

    static public string EscapeXmlString(this string xmlString)
    {
        if (String.IsNullOrEmpty(xmlString))
        {
            return String.Empty;
        }

        return System.Web.HttpUtility.HtmlEncode(xmlString);
    }

    static public string RemoveEndingSlashes(this string str)
    {
        while (str.EndsWith("/"))
        {
            str = str.Substring(0, str.Length - 1);
        }

        return str;
    }

    public static string StripHTMLFromLabel(this string input)
    {
        input = input.Replace("<br>", "\n").Replace("<br/>", "\n");
        if (input.Contains("<") &&
            input.Contains(">") &&
            input.Contains("</"))
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        return input;
    }

    static public string ExtractConnectionStringValue(this string Params, string Param)
    {
        Param = Param.Trim();

        foreach (string a in Params.Split(';'))
        {
            string aa = a.Trim();
            if (aa.ToLower().IndexOf(Param.ToLower() + "=") == 0)
            {
                if (aa.Length == Param.Length + 1)
                {
                    return "";
                }

                return aa.Substring(Param.Length + 1, aa.Length - Param.Length - 1);
            }
        }
        return String.Empty;
    }
}
