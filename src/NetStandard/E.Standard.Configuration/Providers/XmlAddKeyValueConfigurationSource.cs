using Microsoft.Extensions.Configuration;
using System;

namespace E.Standard.Configuration.Providers;

public class XmlAddKeyValueConfigurationSource : IConfigurationSource
{
    private readonly Action<XmlAddKeyValueConfigurationOptions> _optionsAction;

    public XmlAddKeyValueConfigurationSource(Action<XmlAddKeyValueConfigurationOptions> optionsAction)
    {
        _optionsAction = optionsAction;
    }

    #region IConfigurationSource

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        var options = new XmlAddKeyValueConfigurationOptions();
        _optionsAction.Invoke(options);

        return new XmlAddKeyValueConfigurationProvider(options);
    }

    #endregion
}
