using System;

namespace Portal.Core.AppCode.Exceptions;

static public class StringExtensions
{
    private static string[] _malWareStatements = new[] { "<script>", "</script>", "\n" };

    // avoid some send a script via an Url Parameter
    static public string ToStringOrEmptyIfMalware(this object obj)
    {
        var str = obj?.ToString();

        if (string.IsNullOrEmpty(str))
        {
            return String.Empty;
        }

        var checkStr = str.Replace("\t", "").Replace(" ", "");

        foreach (string malWare in _malWareStatements)
        {
            if (checkStr.Contains(malWare, StringComparison.OrdinalIgnoreCase))
            {
                return String.Empty;
            }
        }

        return str;
    }
}
