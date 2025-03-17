using E.Standard.Extensions.Compare;
using E.Standard.Extensions.Text;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace E.Standard.WebGIS.Core.Services;

public class GlobalisationService : IGlobalisationService
{
    private readonly GlobalisationServiceOptions _options;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _languageDirectories;

    public GlobalisationService(IOptions<GlobalisationServiceOptions> options)
    {
        _options = options.Value;
        _languageDirectories = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

        ReadDictionaries();
    }

    #region IGlobalisationService

    public string Get(string key, string language = "")
    {
        if (String.IsNullOrEmpty(key))
        {
            return key;
        }

        language = language?.ToLower().OrTake(_options.DefaultLanguage);

        if (!_languageDirectories.ContainsKey(language))
        {
            return key.CamelCaseToPhrase();
        }

        string dictKey = key.ToLower();

        var languageDictionary = _languageDirectories[language];
        if (!languageDictionary.ContainsKey(dictKey))
        {
            return key.CamelCaseToPhrase();
        }

        return languageDictionary[dictKey];
    }

    public string DefaultLanguage => _options.DefaultLanguage;

    #endregion

    #region Helper

    private void ReadDictionaries()
    {
        try
        {
            foreach (var fileInfo in new DirectoryInfo(_options.RootPath).GetFiles("*.txt"))
            {
                var language = fileInfo.Name.ToLower().Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length);
                var languageDictionary = new ConcurrentDictionary<string, string>();
                var lines = File.ReadAllLines(fileInfo.FullName);
                var text = new StringBuilder();
                var currentValue = String.Empty;

                foreach (var line in lines)
                {
                    if (String.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("#"))
                    {
                        continue;
                    }

                    if (line.StartsWith(" ") || line.StartsWith("\t"))
                    {
                        text.Append(" ");
                        text.Append(line.Trim());
                        continue;
                    }

                    int pos = line.IndexOf(":");
                    if (pos == -1)
                    {
                        continue;
                    }

                    if (!String.IsNullOrEmpty(currentValue) && text.Length > 0)
                    {
                        languageDictionary[currentValue] = text.ToString();
                        text.Clear();
                    }

                    currentValue = line.Substring(0, pos).Trim().ToLower();
                    text.Append(line.Substring(pos + 1).Trim());
                }

                if (!String.IsNullOrEmpty(currentValue) && text.Length > 0)
                {
                    languageDictionary[currentValue] = text.ToString();
                }

                _languageDirectories[language] = languageDictionary;
            }
        }
        catch { }
    }

    #endregion
}
