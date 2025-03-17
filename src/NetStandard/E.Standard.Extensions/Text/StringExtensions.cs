using System;
using System.Collections.Specialized;
using System.Text;

namespace E.Standard.Extensions.Text;

static public class StringExtensions
{
    static public string Replace(this string str, NameValueCollection nvc)
    {
        if (nvc == null || String.IsNullOrEmpty(str))
        {
            return str;
        }

        foreach (var key in nvc.AllKeys)
        {
            str = str.Replace(key, nvc[key]);
        }

        return str;
    }

    static public string CamelCaseToPhrase(this string camelCaseString)
    {
        if (String.IsNullOrEmpty(camelCaseString) || camelCaseString.IsPhrase())
        {
            return camelCaseString;
        }

        StringBuilder phraseBuilder = new StringBuilder();

        for (int i = 0; i < camelCaseString.Length; i++)
        {
            if (i == 0)
            {
                phraseBuilder.Append(char.ToUpper(camelCaseString[i]));
            }
            else if (char.IsUpper(camelCaseString[i]))
            {
                phraseBuilder.Append(" ");
                phraseBuilder.Append(camelCaseString[i]);
            }
            else
            {
                phraseBuilder.Append(camelCaseString[i]);
            }
        }

        return phraseBuilder.ToString();
    }

    static public bool IsPhrase(this string str)
    {
        if (String.IsNullOrEmpty(str))
        {
            return false;
        }

        return str.Contains(" ");
    }

    public static string ReplacePro(this string str, string oldValue, string newValue, StringComparison comparison)
    {
        if (String.IsNullOrEmpty(str) || String.IsNullOrEmpty(oldValue))
        {
            return str;
        }

        StringBuilder sb = new StringBuilder();

        int previousIndex = 0;
        int index = str.IndexOf(oldValue, comparison);

        while (index != -1)
        {
            sb.Append(str.Substring(previousIndex, index - previousIndex));
            sb.Append(newValue);
            index += oldValue.Length;

            previousIndex = index;
            index = str.IndexOf(oldValue, index, comparison);
        }
        sb.Append(str.Substring(previousIndex));

        return sb.ToString();
    }

    public static string RemovePrefixIfPresent(this string str, string prefix)
    {
        if (String.IsNullOrEmpty(str) || String.IsNullOrEmpty(prefix))
        {
            return str;
        }

        if (str.StartsWith(prefix))
        {
            return str.Substring(prefix.Length);
        }

        return str;
    }

    public static string DoubleToMinLength(this string str, int minLength)
    {
        if (String.IsNullOrEmpty(str))
        {
            return str;
        }

        while (str.Length <= minLength)
        {
            str += str;
        }

        return str;
    }
}
