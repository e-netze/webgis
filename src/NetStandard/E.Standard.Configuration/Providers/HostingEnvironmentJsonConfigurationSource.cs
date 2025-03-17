using Microsoft.Extensions.Configuration;

namespace E.Standard.Configuration.Providers;

public class HostingEnvironmentJsonConfigurationSource : IConfigurationSource
{
    #region IConfigurationSource

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new HostingEnvironmentJsonConfigurationProvider();
    }

    #endregion
}
