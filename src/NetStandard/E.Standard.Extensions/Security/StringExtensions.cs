using E.Standard.Extensions.Compare;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace E.Standard.Extensions.Security;

static public class StringExtensions
{
    public static bool ContainsMaliciousJavaScript(this string input)
    {
        if (input == null)
        {
            return false;
        }

        string pattern = @"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>";

        return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
    }

    public static string CheckSerucity(this string input)
    {
        input.ContainsMaliciousJavaScript()
             .ThrowIfTrue(() => $"Input contains potentially malicious javascript {input.Substring(0, Math.Min(12, input.Length))}...");

        return input;
    }

    public static void CheckSecurity(this IEnumerable<string> input)
    {
        if (input == null)
        {
            return;
        }

        foreach (var testString in input)
        {
            testString.CheckSerucity();
        }
    }

    public static bool IsUrl(this string input)
    {
        if (input == null)
        {
            return false;
        }

        bool result = Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult) && uriResult?.Scheme == Uri.UriSchemeHttp || uriResult?.Scheme == Uri.UriSchemeHttps;

        return result;
    }

    public static bool IsUrlOrContainsBacks(this string input)
    {
        if (input == null)
        {
            return false;
        }

        return input.IsUrl() || input.Contains("../");
    }

    public static string UrlToHash(this string url)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        string hash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(url)));
        return hash;
    }
}
