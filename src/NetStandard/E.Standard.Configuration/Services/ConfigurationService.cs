using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Configuration.Services;

public class ConfigurationService
{
    private readonly IEnumerable<IConfigValueParser> _configValuleParsers;

    public ConfigurationService(IConfiguration configuration, IEnumerable<IConfigValueParser> configValueParsers)
    {
        Configuration = configuration;
        _configValuleParsers = configValueParsers;
    }

    public IConfiguration Configuration { get; private set; }

    public string this[string key]
    {
        get
        {
            if (String.IsNullOrEmpty(key))
            {
                return String.Empty;
            }

            var configValue = this.Configuration[key];

            if (configValue != null)
            {
                foreach (var configValueParser in _configValuleParsers)
                {
                    configValue = configValueParser.Parse(configValue);
                }

                // Set Parsed Value  (only query external services like keyvaults for the first use)
                if (this.Configuration[key] != configValue)
                {
                    this.Configuration[key] = configValue;
                }
            }

            return configValue ?? String.Empty;
        }
    }

    public T Get<T>(string key)
    {
        T defaultValue = default(T);

        if (typeof(T) == typeof(bool))
        {
            defaultValue = (T)Convert.ChangeType(false, typeof(T));  // Falls false nicht sowieso default(T) ist??
        }

        return Get<T>(key, default);
    }

    public T Get<T>(string key, T defaultValue)
    {
        var value = this[key];

        if (String.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public T TryGet<T>(string key, T defaultValue)
    {
        try
        {
            return Get<T>(key, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }

    public IEnumerable<T> GetValues<T>(string key, char separator = ',')
    {
        var value = this[key];

        if (!String.IsNullOrEmpty(value))
        {
            return value.Split(separator)
                        .Select(a => a.Trim())
                        .Where(a => !String.IsNullOrEmpty(a))
                        .Select(a => (T)Convert.ChangeType(a, typeof(T)));
        }

        return new T[0];
    }

    public bool HasValue<T>(string key, T value, char separator = ',')
    {
        return GetValues<T>(key, separator).Contains(value);
    }

    public IEnumerable<string> GetPathsStartWith(string key)
    {
        IEnumerable<IConfigurationSection> children;
        if (key.Contains(":"))
        {
            var sectionName = key.Substring(0, key.LastIndexOf(":"));
            var section = Configuration.GetSection(sectionName);
            children = section.GetChildren();
        }
        else
        {
            children = Configuration.GetChildren();
        }

        return children.Where(c => c.Path.StartsWith(key))
                       .Select(c => c.Path);
    }
}
