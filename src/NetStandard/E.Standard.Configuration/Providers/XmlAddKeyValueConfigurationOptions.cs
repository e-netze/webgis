using System.Collections.Generic;

namespace E.Standard.Configuration.Providers;

public class XmlAddKeyValueConfigurationOptions
{
    public XmlAddKeyValueConfigurationOptions()
    {
        SectionName = "Application";
    }

    public string SectionName { get; set; }
    public string ConfigurationName { get; set; }

    public delegate void OnConfigKeyAddedDelegate(string key, IDictionary<string, string> data);
    public OnConfigKeyAddedDelegate OnConfigKeyAdded;
}
