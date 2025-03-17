using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Xml;

namespace E.Standard.Configuration.Providers;

public class XmlAddKeyValueConfigurationProvider : ConfigurationProvider
{
    private XmlAddKeyValueConfigurationOptions _options;

    public XmlAddKeyValueConfigurationProvider(XmlAddKeyValueConfigurationOptions options)
    {
        _options = options;
    }

    public override void Load()
    {
        AppConfiguration appConfiguration = new AppConfiguration(_options.ConfigurationName);

        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.Load(appConfiguration.ConfigurationFile);

        this.Data = new Dictionary<string, string>();

        AddSection(xmlDocument);

        foreach (XmlNode xmlNode in xmlDocument.SelectNodes("configuration/appSettings/section[@name]"))
        {
            AddSection(xmlDocument, xmlNode.Attributes["name"].Value);
        }
    }

    private void AddSection(XmlDocument xmlDocument, string subSectionName = "")
    {
        string xPath = String.IsNullOrWhiteSpace(subSectionName) ?
            "configuration/appSettings/add[@key and @value]" :
            $"configuration/appSettings/section[@name='{subSectionName}']/add[@key and @value]";

        foreach (XmlNode xmlNode in xmlDocument.SelectNodes(xPath))
        {
            string key = String.IsNullOrWhiteSpace(subSectionName) ?
                $"{_options.SectionName}:{xmlNode.Attributes["key"].Value}" :
                $"{_options.SectionName}:{subSectionName}:{xmlNode.Attributes["key"].Value}";

            string val = xmlNode.Attributes["value"].Value;

            if (!Data.ContainsKey(key))
            {
                Data.Add(key, val);

                _options.OnConfigKeyAdded?.Invoke(key, Data);
            }
        }
    }
}
