using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

using E.Standard.Localization.Models;

using Microsoft.Extensions.Localization;

/// <summary>
/// 
/// Loads translations from Markdown files based on the specified language. It provides localized strings for headers
/// and bodies.
/// 
/// Example of a Markdown file: redlinig.en.md
/// #mapmarkup: Map-Markup
/// ##tools: Tools
/// ###drawline: Draw Line
/// ####note1: Notice
/// Please draw a line with at least two support points.
/// ####note2: Notice
/// The line must not intersect itself.
/// 
/// usage:
/// Inject IStringLocalizer _localizer into your service.
/// 
/// _localizer["mapmarkup.tools.drawline"] returns "Draw Line"
/// _localizer["mapmarkup.tools.drawline.note1:body"] returns "Please draw a line with at least two support points."
/// 
/// </summary>

public class MarkdownLocalizer : IStringLocalizer
{
    private static readonly ConcurrentDictionary<(string Language, string ResourcePath, string OverridePath), Translations> LanguageDictionaries = new();

    private readonly Translations _translations;

    public MarkdownLocalizer(string language, string resourcePath = "", string overridePath = "")
    {
        ResourcePath = String.IsNullOrEmpty(resourcePath) ? "l10n" : resourcePath;
        OverridePath = overridePath;

        var cacheKey = (
            language,
            Path.GetFullPath(ResourcePath),
            String.IsNullOrEmpty(OverridePath) ? String.Empty : Path.GetFullPath(OverridePath));

        _translations = LanguageDictionaries.GetOrAdd(cacheKey, _ => LoadTranslations(language));
    }

    public string ResourcePath { get; }

    public string OverridePath { get; }

    private Translations LoadTranslations(string language)
    {
        var result = new Translations();

        LoadTranslationsFromDirectory(result, ResourcePath, language);

        if (!String.IsNullOrWhiteSpace(OverridePath))
        {
            LoadTranslationsFromDirectory(result, OverridePath, language);
        }

        return result;
    }

    private static void LoadTranslationsFromDirectory(Translations result, string resourcePath, string language)
    {
        var diInfo = new DirectoryInfo(Path.Combine(resourcePath, language));

        if (!diInfo.Exists)
        {
            return;
        }

        foreach (var fi in diInfo.GetFiles($"*.md"))
        {
            string fileNameSpace = $"{fi.Name.Replace($".md", "").ToLowerInvariant()}";

            if (!String.IsNullOrEmpty(fileNameSpace))  // default filename ".md" => no namespace
            {
                fileNameSpace += ".";
            }

            var lines = File.ReadAllLines(fi.FullName, Encoding.UTF8);
            string currentKey = "";
            string currentHeader = "";
            StringBuilder currentBody = new();

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^(#+)([^:]+):\s*(.*)$");

                if (match.Success)
                {
                    // save current entry
                    if (!string.IsNullOrEmpty(currentKey))
                    {
                        result[$"{fileNameSpace}{currentKey}"] = (currentHeader, currentBody.ToString().Trim());
                    }

                    // define new entry
                    string level = match.Groups[1].Value; // #, ##, ### etc.
                    string keyPart = match.Groups[2].Value.Trim();
                    string header = match.Groups[3].Value.Trim().Replace("\\n", "\n");

                    currentKey = String.Join(".", currentKey.Split('.').Take(level.Length - 1));

                    // key is composed of all parts joined by dots
                    currentKey = (currentKey == ""
                        ? keyPart
                        : $"{currentKey}.{keyPart}").ToLower();

                    currentHeader = header;
                    currentBody.Clear();
                }
                else // if (!string.IsNullOrWhiteSpace(line))
                {
                    currentBody.Append($"{line.Replace("\\n", "\n")}\n");
                }
            }

            // save last entry
            if (!string.IsNullOrEmpty(currentKey))
            {
                result[$"{fileNameSpace}{currentKey}"] = (currentHeader, currentBody.ToString().Trim());
            }
        }
    }

    public LocalizedString this[string name]
    {
        get
        {
            if (String.IsNullOrEmpty(name))
            {
                return new LocalizedString("", "", resourceNotFound: true);
            }

            string lookupKey = name.ToLower();
            bool isBodyRequest = lookupKey.EndsWith(":body");
            string actualKey = isBodyRequest ? lookupKey.Replace(":body", "") : lookupKey;

            if (_translations.TryGetValue(actualKey, out var value))
            {
                return new LocalizedString(name, isBodyRequest ? value.Body : value.Header);
            }

            return new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments] => this[name];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _translations.Select(t => new LocalizedString(t.Key, t.Value.Header));
    }
}
