using System;

using E.Standard.Configuration.Providers;

using Microsoft.Extensions.Configuration;

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

    /// <summary>
    /// Adds an optional, reloadable JSON file from the "_config" directory (e.g.
    /// "_config/logging.json") to the configuration. This lets administrators who only have
    /// access to the mounted "_config" directory (a common Kubernetes ConfigMap-volume setup)
    /// configure anything reachable through Microsoft.Extensions.Configuration - Logging,
    /// Serilog, OpenTelemetry (OTEL_* keys), etc. - without touching environment variables or
    /// appsettings.json. A missing file is a no-op.
    /// </summary>
    static public IConfigurationBuilder AddConfigDirectoryJsonFile(this IConfigurationBuilder builder, string fileName)
    {
        return builder.AddJsonFile(ConfigDirectory.ResolveFilePath(fileName), optional: true, reloadOnChange: true);
    }
}
