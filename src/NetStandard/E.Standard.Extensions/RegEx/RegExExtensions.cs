using System.Text.RegularExpressions;

namespace E.Standard.Extensions.RegEx;

static public class RegExExtensions
{
    public static string WildCardToRegular(this string value)
    {
        value = value.Replace("%", "*");

        // If you want to implement both "*" and "?"
        //return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";

        // If you want to implement "*" only
        return $"^{Regex.Escape(value).Replace("\\*", ".*")}$";
    }

    public static bool MatchWildcard(this string value, string pattern)
    {
        pattern = WildCardToRegular(pattern);
        return Regex.IsMatch(value, pattern);
    }
}