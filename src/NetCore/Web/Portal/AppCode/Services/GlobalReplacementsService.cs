using Microsoft.Extensions.Configuration;
using Portal.Core.AppCode.Configuration;
using System;
using System.Collections.Concurrent;

namespace Portal.Core.AppCode.Services;

public class GlobalReplacementsService
{
    private readonly ConcurrentDictionary<string, string> _globalReplacements;

    public GlobalReplacementsService(IConfiguration config)
    {
        _globalReplacements = new();

        try
        {
            foreach (var sectionName in new[] { "global-replacements" })
            {
                var section = config.GetSection($"{PortalConfigKeys.ConfigurationSectionName}:{sectionName}");

                foreach (var redirection in section.GetChildren())
                {
                    string val = redirection.Value;

                    if (!String.IsNullOrEmpty(val) && val.Contains(" => "))
                    {
                        int pos = val.IndexOf(" => ");
                        string from = val.Substring(0, pos).Trim().ToLower();
                        string to = val.Substring(pos + 4).Trim();

                        _globalReplacements.TryAdd(from, to);
                    }
                }
            }
        }
        catch { }
    }

    public string Apply(string source)
    {

        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        foreach (var from in _globalReplacements.Keys)
        {
            source = source.Replace(from, _globalReplacements[from]);
        }

        return source;
    }
}
