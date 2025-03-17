using System.Text.RegularExpressions;

namespace E.Standard.WebGIS.Tools.Editing.Extensions;

static internal class StringExtensions
{
    public static string ReplaceSingleBackslashesToDoubleBackslashes(this string input)
    {
        // only replace, if it is a single backslash: a backslash not followed by another 
        return Regex.Replace(input, @"(?<!\\)\\(?!\\)", @"\\");
    }
}
