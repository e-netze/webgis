using E.Standard.ActiveDirectory;
using E.Standard.Configuration.Extensions.DependencyInjection;
using E.Standard.Json;
using E.Standard.WebGIS.Core;
using Microsoft.Extensions.Hosting;
using Portal.Core.AppCode.Configuration;
using System;

namespace Portal.Core.AppCode.Extensions.DependencyInjection;

static internal class HostBuilderExtensions
{
    static public TBuilder PerformWebgisPortalSetup<TBuilder>(this TBuilder builder, string[] args)
        where TBuilder : IHostApplicationBuilder
    {
        #region Config App Globals Settings

        ActiveDirectoryFactory.AddInterfaceImplementation<IAdQuery>(typeof(E.Standard.ActiveDirectory.AdQuery));

        // Micosoft Bug mit .NET Core 2.1 -> Verbindungen mit Proxy erzeugen error
        // https://github.com/dotnet/corefx/issues/30166
        // AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
        System.Net.WebRequest.DefaultWebProxy = null;

#pragma warning disable SYSLIB0014
        System.Net.ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
        System.Net.ServicePointManager.SecurityProtocol |=
                    System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
#pragma warning restore SYSLIB0014

        HmacResponseDTO.CurrentVersion = HmacVersion.V2;

        JSerializer.SetEngine("Newtonsoft".Equals(builder.Configuration["JsonSerializationEngine"], StringComparison.OrdinalIgnoreCase)
            ? JsonEngine.NewtonSoft
            : JsonEngine.SytemTextJson);

        #endregion

        #region First Start => init configuration

        new SimpleSetup().TrySetup(args);

        #endregion

        return builder;
    }

    static public TBuilder AddWebgisPortalConfiguration<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Configuration.AddXmlAddKeyValueConfiguration(options =>
        {
            var configName = "portal.config";
            var configNamePostfix = GetEnvironmentVariable("WEBGIS_PORTAL_CONFIG_NAME");  // zB Staging/Test Instances...
            if (!String.IsNullOrWhiteSpace(configNamePostfix))
            {
                configName = $"portal-{configNamePostfix}.config";
            }

            options.ConfigurationName = configName;
            options.SectionName = PortalConfigKeys.ConfigurationSectionName;
        });

        builder.Configuration.AddHostingEnviromentXmlConfiguration();

        return builder;
    }

    #region Helper

    static private string GetEnvironmentVariable(string name, string defaultValue = "")
    {
        try
        {
            var environmentVariables = System.Environment.GetEnvironmentVariables();
            if (!environmentVariables.Contains(name) || String.IsNullOrWhiteSpace(environmentVariables[name]?.ToString()))
            {
                return defaultValue;
            }

            return environmentVariables[name].ToString();
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion
}
