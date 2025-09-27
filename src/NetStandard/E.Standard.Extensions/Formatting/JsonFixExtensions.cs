using System;
using System.Text.RegularExpressions;

namespace E.Standard.Extensions.Formatting;

public static class JsonFixExtensions
{
    // Reasonable default timeout to prevent pathological inputs from hanging (security)
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);

    // Size limit for incoming JSON-like payloads (adjust to your needs) (security)
    private const int MaxLen = 64 * 1024; // 64 KB

    // Compiled regexes with explicit timeouts
    private static readonly Regex UnquotedKeyRegex = new(
        pattern: @"(?<={|,)(?<lead>\s*)(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*:",
        options: RegexOptions.Compiled,
        matchTimeout: RegexTimeout);

    private static readonly Regex SingleQuotedStringRegex = new(
        pattern: @"'((?:\\.|[^'\\])*)'",
        options: RegexOptions.Compiled,
        matchTimeout: RegexTimeout);

    /// <summary>
    /// Converts a JSON-like string into strict JSON:
    /// 1) quotes unquoted property names
    /// 2) converts single-quoted strings to double-quoted strings
    /// Includes basic hardening: size limit and regex timeouts.
    /// </summary>
    public static string FixToStrictJson(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        if (input.Length > MaxLen)
            throw new ArgumentException($"Input too large (>{MaxLen} chars).");

        // Quick pre-checks to avoid regex work when unnecessary
        bool looksLikeObjectOrArray = input.IndexOf('{') >= 0 || input.IndexOf('[') >= 0;
        bool hasColon = input.IndexOf(':') >= 0;
        if (!looksLikeObjectOrArray || !hasColon)
            return input;

        string withQuotedKeys = input;

        // Only run key-fix if it looks like there are unquoted keys (heuristic)
        // E.g., find a colon with a preceding word char and no preceding quote
        if (MightHaveUnquotedKeys(input))
        {
            withQuotedKeys = UnquotedKeyRegex.Replace(input, m =>
                $"{m.Groups["lead"].Value}\"{m.Groups["key"].Value}\":");
        }

        // Only run string-fix if single quotes appear
        if (withQuotedKeys.IndexOf('\'') >= 0)
        {
            withQuotedKeys = SingleQuotedStringRegex.Replace(withQuotedKeys, m =>
            {
                var content = m.Groups[1].Value;
                content = content.Replace(@"\'", "'");  // unescape \' to literal '
                content = content.Replace("\"", "\\\""); // escape double quotes
                return $"\"{content}\"";
            });
        }

        return withQuotedKeys;
    }

    // Heuristic: colon that is preceded by word char and not by a quote within a short lookbehind window
    private static bool MightHaveUnquotedKeys(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == ':')
            {
                // Walk left to skip spaces
                int j = i - 1;
                while (j >= 0 && char.IsWhiteSpace(s[j])) j--;
                if (j >= 0 && (char.IsLetterOrDigit(s[j]) || s[j] == '_'))
                {
                    // If the previous non-space char before the key isn't a quote, assume unquoted key
                    // (Cheap heuristic; fine for deciding to run the regex)
                    return true;
                }
            }
        }
        return false;
    }
}
