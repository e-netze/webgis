using E.Standard.Localization.Models;
using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 
/// Loads translations from Markdown files based on the specified language. It provides localized strings for headers
/// and bodies.
/// 
/// Example of a Markdown file: redlinig.en.md
/// #redlining: Redlining
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
/// _localizer["redlining.tools.drawline"] returns "Draw Line"
/// _localizer["redlining.tools.drawline.note1:body"] returns "Please draw a line with at least two support points."
/// 
/// </summary>

class MarkdownLocalizer : IStringLocalizer
{
    private static ConcurrentDictionary<string, Translations> LanguageDictionaries = new();

    private Translations _translations;

    public MarkdownLocalizer(string language, string resourcePath = "")
    {
        if (!String.IsNullOrEmpty(resourcePath))
        {
            ResourcePath = resourcePath;
        }

        if (!LanguageDictionaries.TryGetValue(language, out _translations))
        {
            LanguageDictionaries[language] = LoadTranslations(language) ?? new();
        }

        _translations = LanguageDictionaries[language];
    }

    public string ResourcePath { get; } = "l10n";

    private Translations? LoadTranslations(string language)
    {
        var diInfo = new DirectoryInfo(Path.Combine(ResourcePath, language));

        if (!diInfo.Exists)
        {
            return null;
        }

        var result = new Translations();

        foreach (var fi in diInfo.GetFiles($"*.md"))
        {
            string fileNameSpace = $"{fi.Name.Replace($".md", "").ToLowerInvariant()}";

            if(!String.IsNullOrEmpty(fileNameSpace))  // default filename ".md" => no namespace
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
                    string header = match.Groups[3].Value.Trim();

                    currentKey = String.Join(".", currentKey.Split('.').Take(level.Length - 1));

                    // key is composed of all parts joined by dots
                    currentKey = (currentKey == ""
                        ? keyPart
                        : $"{currentKey}.{keyPart}").ToLower();

                    currentHeader = header;
                    currentBody.Clear();
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    currentBody.Append($"{line}\n");
                }
            }

            // save last entry
            if (!string.IsNullOrEmpty(currentKey))
            {
                result[$"{fileNameSpace}{currentKey}"] = (currentHeader, currentBody.ToString().Trim());
            }
        }

        return result;
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
