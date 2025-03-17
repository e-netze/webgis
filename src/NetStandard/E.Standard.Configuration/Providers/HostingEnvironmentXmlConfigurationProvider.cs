using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Xml;

namespace E.Standard.Configuration.Providers;

public class HostingEnvironmentXmlConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        AppConfiguration appConfiguration = new AppConfiguration($"env-{Environment.MachineName}.config");

        if (appConfiguration.Exists)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(appConfiguration.ConfigurationFile);

            this.Data = new Dictionary<string, string>();

            foreach (XmlNode xmlNode in xmlDocument.SelectNodes("configuration/appSettings/add[@key and @value]"))
            {
                string key = $"hostingenvironment-{Environment.MachineName}:{xmlNode.Attributes["key"].Value}";
                string val = xmlNode.Attributes["value"].Value;

                if (!Data.ContainsKey(key))
                {
                    Data.Add(key, val);
                }
            }
        }
    }
}
