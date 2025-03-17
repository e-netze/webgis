using E.Standard.Security.App.KeyVault;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace E.Standard.Security.App;

public class ConfigSecurityHandler
{
    public ConfigSecurityHandler(IKeyVault keyVaule)
    {
        this.KeyVault = keyVaule;
    }

    private IKeyVault KeyVault { get; set; }

    public void ParseConfigProperties(object obj)
    {
        if (obj == null)
        {
            return;
        }
        else if (obj is IConfiguration)
        {
            ParseConfigurtaion((IConfiguration)obj);
        }
    }

    public void ParseConfigurtaion(IConfiguration configuration)
    {
        if (configuration is IConfigurationSection)
        {
            ParseConfiguration((IConfigurationSection)configuration);
        }

        foreach (var section in configuration.GetChildren())
        {
            ParseConfiguration(section);
        }
    }

    private void ParseConfiguration(IConfigurationSection section)
    {
        foreach (var childSection in section.GetChildren())
        {
            ParseConfiguration(childSection);
        }

        if (section.Value != null)
        {
            section.Value = ParseConfigurationValue(section.Value);
        }
    }

    public string ParseConfigurationValue(string configValue)
    {
        if (!String.IsNullOrEmpty(configValue))
        {
            if (configValue.StartsWith("kv:"))
            {
                if (KeyVault == null)
                {
                    throw new Exception("KeyVault is not definied");
                }
                configValue = this.KeyVault.Secret(configValue.Substring("kv:".Length));
            }
        }

        return configValue;
    }

    #region Static Members

    static public bool IsDeveloperMode = false;

    static public string GetGlobalConfigString(string name, string defaultValue = "")
    {
        var ret = String.Empty;
#if DEBUG
        if (IsDeveloperMode)
        {
            foreach (var line in System.IO.File.ReadAllLines(@"C:\temp\webportal_config.txt"))
            {
                if (line.StartsWith(name + "="))
                {
                    ret = line.Substring((name).Length + 1);
                }
            }
        }
#else
        ret = GetEnvironmentVariable(name, defaultValue);
#endif

        if (String.IsNullOrWhiteSpace(ret))
        {
            return defaultValue;
        }

        return ret;
    }

    #endregion

    #region Environment

    public Dictionary<string, string> EnvironmentVariablesPreview
    {
        get
        {
            var environmentVariables = Environment.GetEnvironmentVariables();
            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (string key in environmentVariables.Keys)
            {
                if (key.ToLower().Contains("password"))
                {
                    ret[key] = "****************";
                }
                else
                {
                    string val = environmentVariables[key]?.ToString();
                    ret[key] = val != null ?
                        val.Substring(0, Math.Min(val.Length / 2, 50)) + "*******************************" :
                        "<null>";
                }
            }

            return ret;
        }
    }

    static private string GetEnvironmentVariable(string name, string defaultValue = "")
    {
        var environmentVariables = Environment.GetEnvironmentVariables();
        if (!environmentVariables.Contains(name) || String.IsNullOrWhiteSpace(environmentVariables[name]?.ToString()))
        {
            return defaultValue;
        }

        return environmentVariables[name].ToString();
    }

    #endregion
}
