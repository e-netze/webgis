using System;
using System.Linq;

namespace E.Standard.Extensions.Formatting;

static public class FormattingExtensions
{
    static public string ToInvariantNumberString<T>(this string number)
    {
        if (typeof(T) == typeof(double) ||
           typeof(T) == typeof(float) ||
           typeof(T) == typeof(decimal))
        {
            number = number?.Replace(",", ".");
        }

        return number;
    }

    static public string ClassName(this object obj)
    {
        if (obj == null)
        {
            return String.Empty;
        }

        string name = obj.GetType().ToString();

        if (name.Contains("."))
        {
            name = name.Substring(name.LastIndexOf(".") + 1);
        }

        return name;
    }

    static public bool IsNumber(this string val)
    {
        val = val.Trim();

        if (String.IsNullOrEmpty(val))
        {
            return false;
        }

        return val.All(c => Char.IsNumber(c) || c == '.' || c == ',' || c == '+' || c == '-');
    }

    static public string ToValidFilename(this string filename)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            filename = filename.Replace(c, '_');
        }

        return filename;
    }
}
