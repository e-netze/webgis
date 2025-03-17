using Microsoft.Extensions.Configuration;

namespace E.Standard.Configuration.Providers;

public class HostingEnvironmentXmlConfigurationSource : IConfigurationSource
{
    #region IConfigurationSource

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new HostingEnvironmentXmlConfigurationProvider();
    }

    #endregion
}
