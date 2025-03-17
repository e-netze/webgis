using System;

namespace E.Standard.Configuration;

public class ConfigurationNameAttribute : Attribute
{
    public ConfigurationNameAttribute(string configName)
    {
        this.ConfigName = configName;
    }

    public string ConfigName { get; private set; }
}
