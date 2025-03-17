using E.Standard.Configuration.Providers;
using Microsoft.Extensions.Configuration;
using System;

namespace E.Standard.Configuration.Extensions.DependencyInjection;

static public class ConfiguraitonBuilderExtensions
{
    static public IConfigurationBuilder AddXmlAddKeyValueConfiguration(
                                            this IConfigurationBuilder builder,
                                            Action<XmlAddKeyValueConfigurationOptions> opitonsAction)
    {
        return builder.Add(new XmlAddKeyValueConfigurationSource(opitonsAction));
    }

    static public IConfigurationBuilder AddHostingEnviromentXmlConfiguration(this IConfigurationBuilder builder)
    {
        return builder.Add(new HostingEnvironmentXmlConfigurationSource());
    }

    static public IConfigurationBuilder AddHostingEnviromentJsonConfiguration(this IConfigurationBuilder builder)
    {
        return builder.Add(new HostingEnvironmentJsonConfigurationSource());
    }
}
