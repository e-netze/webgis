using E.Standard.Security.App.Services.Abstraction;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace E.Standard.Azure.Services.Parser;

public class AzureKeyVaultConfigValueParser : IConfigValueParser
{
    private readonly AzureKeyVaultConfigValueParserOptions _options;
    private KeyVault.KeyVault _keyVault = null;
    public AzureKeyVaultConfigValueParser(IOptions<AzureKeyVaultConfigValueParserOptions> options)
    {
        _options = options.Value;
    }

    #region IConfigValueParser

    public string Parse(string configValue)
    {
        if (!configValue.Contains("kv:"))
        {
            return configValue;
        }

        _keyVault = _keyVault ?? new KeyVault.KeyVault(_options.KeyValueUri, _options.TenantId, _options.ClientId, _options.ClientSecret);

        if (configValue.StartsWith("kv:"))
        {
            configValue = _keyVault.Secret(configValue.Substring("kv:".Length));
        }
        if (configValue.Contains("{{") && configValue.Contains("}}"))
        {
            foreach (Match match in Regex.Matches(configValue, @"\{{(.*?)\}}"))
            {
                var matchKey = match.Value.Substring(2, match.Value.Length - 4);

                if (matchKey.StartsWith("kv:"))
                {
                    string kvValue = _keyVault.Secret(matchKey.Substring("kv:".Length));
                    configValue = configValue.Replace(match.Value, kvValue);
                }
            }
        }

        return configValue;
    }

    #endregion
}
