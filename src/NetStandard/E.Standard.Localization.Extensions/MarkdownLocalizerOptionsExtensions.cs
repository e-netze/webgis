using E.Standard.Localization.Services;
using System.Globalization;

namespace E.Standard.Localization.Extensions;
static public class MarkdownLocalizerOptionsExtensions
{
    static public IDictionary<string, string> SupportedLanguageDictionary(this MarkdownLocalizerOptions options)
    {
        var result = new Dictionary<string, string>();

        foreach (var culture in options.SupportedLanguages)
        {
            try
            {
                var ci = CultureInfo.GetCultureInfo(culture);
                result.Add(culture,
                    ci.NativeName != ci.EnglishName
                      ? $"{ci.NativeName} ({ci.EnglishName})"
                      : ci.NativeName);
            }
            catch (CultureNotFoundException)
            {
                continue;
            }
        }

        return result;
    }
}
