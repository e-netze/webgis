using E.Standard.Security.App.Services.Abstraction;

namespace E.Standard.Configuration.Services;

public class SecurityConfigurationService : ISecurityConfigurationService
{
    private readonly ConfigurationService _config;

    public SecurityConfigurationService(ConfigurationService config)
    {
        _config = config;
    }

    #region ISecurityConfigurationService

    public string this[string key] => _config[key];

    #endregion
}
