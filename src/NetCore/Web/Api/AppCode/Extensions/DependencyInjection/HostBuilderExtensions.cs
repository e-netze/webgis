using E.Standard.Api.App.Configuration;
using E.Standard.Configuration.Extensions.DependencyInjection;
using E.Standard.Json;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Xml;

namespace Api.Core.AppCode.Extensions.DependencyInjection;

static internal class HostBuilderExtensions
{
    static public TBuilder PerformWebgisApiSetup<TBuilder>(this TBuilder builder, string[] args)
        where TBuilder : IHostApplicationBuilder
    {
        #region Config App Globals Settings

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

        new Core.AppCode.SimpleSetup().TrySetup(args);

        #endregion

        #region Create Folders

        builder.Configuration.TryCreateDirectoryIfNotExistes(ApiConfigKeys.ToKey("outputPath"));
        builder.Configuration.TryCreateDirectoryIfNotExistes(ApiConfigKeys.ToKey("server-side-configuration-path"));
        builder.Configuration.TryCreateDirectoryIfNotExistes(ApiConfigKeys.ToKey("cache-connectionstring"));
        builder.Configuration.TryCreateDirectoryIfNotExistes(ApiConfigKeys.ToKey("subscriber-db-connectionstring"));
        builder.Configuration.TryCreateDirectoryIfNotExistes(ApiConfigKeys.ToKey("storage-rootpath"));

        #endregion

        return builder;
    }

    static public TBuilder AddWebgisApiConfiguration<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Configuration.AddXmlAddKeyValueConfiguration(options =>
        {
            string configName = "api.config";
            var configNamePostfix = GetEnvironmentVariable("WEBGIS_API_CONFIG_NAME");  //// zB Staging/Test or Datalinq Instances...
            if (!String.IsNullOrWhiteSpace(configNamePostfix))
            {
                configName = $"api-{configNamePostfix}.config";
            }

            options.ConfigurationName = configName;
            options.SectionName = ApiConfigKeys.ConfigurationSectionName;
            options.OnConfigKeyAdded = (key, data) =>
            {
                if (key == ApiConfigKeys.ServerSideConfigurationPath)
                {
                    #region Read Legacy dotNETConnector.xml Proxy, etc

                    var serverSideConfigurationPath = data[ApiConfigKeys.ServerSideConfigurationPath];
                    try
                    {
                        var fi = new FileInfo($"{serverSideConfigurationPath}/config/ims/dotNETConnector.xml");
                        if (fi.Exists)
                        {
                            XmlDocument doc = new XmlDocument();
                            doc.Load(fi.FullName);

                            XmlNodeList pr = doc.GetElementsByTagName("proxy");
                            if (pr.Count > 0)
                            {
                                if (Convert.ToBoolean(pr[0].Attributes["use"].Value) == true)
                                {
                                    data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy-proxy:use", "true");

                                    data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy-proxy:user", pr[0].Attributes["user"] != null ? pr[0].Attributes["user"].Value : String.Empty);
                                    data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy-proxy:pwd", pr[0].Attributes["passwd"] != null ? pr[0].Attributes["passwd"].Value : String.Empty);
                                    data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy-proxy:domain", pr[0].Attributes["domain"] != null ? pr[0].Attributes["domain"].Value : String.Empty);
                                    data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy-proxy:server", pr[0].Attributes["proxyserver"]?.Value ?? String.Empty);
                                    data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy-proxy:port", pr[0].Attributes["proxyport"]?.Value ?? "80");

                                    data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy-proxy:ignore", pr[0].Attributes["ignore"]?.Value);
                                }
                            }

                            XmlNodeList alwaysdownload = doc.GetElementsByTagName("alwaysdownloadfrom");
                            if (alwaysdownload.Count > 0)
                            {
                                data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy:alwaysdownloadfrom", alwaysdownload[0].Attributes["value"]?.Value);
                            }

                            int counter = 0;
                            foreach (XmlNode redirectNode in doc.SelectNodes("config/output/redirections/redirection[@from and @to]"))
                            {
                                data.Add($"{ApiConfigKeys.ConfigurationSectionName}:legacy-url-redirections:redirect{++counter}",
                                    $"{redirectNode.Attributes["from"].Value} => {redirectNode.Attributes["to"].Value}");
                            }
                        }
                    }
                    catch { }

                    #endregion
                }
            };
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
