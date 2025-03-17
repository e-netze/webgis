using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.RegularExpressions;

namespace E.Standard.Configuration.Services.Parser;

public class HostingEnvironmentConfigValueParser : IConfigValueParser
{
    private readonly IConfiguration _configuration;

    public HostingEnvironmentConfigValueParser(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    #region IConfigValueParser

    public string Parse(string configValue)
    {
        if (configValue.Contains("{{") && configValue.Contains("}}"))
        {
            foreach (Match match in Regex.Matches(configValue, @"\{{(.*?)\}}"))
            {
                var matchKey = match.Value.Substring(2, match.Value.Length - 4);

                if (matchKey.StartsWith("env:"))
                {
                    var envValue = _configuration[$"hostingenvironment-{Environment.MachineName}:{matchKey.Substring(4)}"];
                    configValue = configValue.Replace(match.Value, envValue);
                }
            }
        }

        return configValue;
    }

    #endregion
}
