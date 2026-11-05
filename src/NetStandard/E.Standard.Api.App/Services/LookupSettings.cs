using System;
using System.Text.RegularExpressions;

namespace E.Standard.Api.App.Services;

/// <summary>
/// Settings for <see cref="LookupService"/> (lookup / autocomplete queries).
/// </summary>
/// <remarks>
/// Configurable via <c>api.config</c>:
/// <list type="bullet">
/// <item><c>lookup:term-whitelist</c> → <see cref="TermWhiteList"/></item>
/// </list>
/// </remarks>
public static class LookupSettings
{
    public const string DefaultTermWhiteList = @"^[\p{L}\p{N} _.,%/#:()+&@'\-]{0,120}$";

    private static Regex _termWhiteList = new Regex(DefaultTermWhiteList, RegexOptions.Compiled);

    /// <summary>
    /// Regular expression a lookup search term (and parameter replacement values) must match.
    /// Single quotes are always escaped by the service, no matter what this expression allows.
    /// </summary>
    public static string TermWhiteList
    {
        get => _termWhiteList.ToString();
        set
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return;
            }

            try
            {
                _termWhiteList = new Regex(value, RegexOptions.Compiled);
            }
            catch (ArgumentException)
            {
                // invalid expression: keep the current (safe) one
            }
        }
    }

    internal static bool IsValidTerm(string term) => _termWhiteList.IsMatch(term ?? String.Empty);
}
