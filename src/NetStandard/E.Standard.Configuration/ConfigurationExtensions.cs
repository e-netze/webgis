using System;
using System.Linq;
using System.Reflection;

namespace E.Standard.Configuration;

static public class ConfigurationExtensions
{
    static public string ConfigurationValue(this IConfigurationConsumer consumer, string key)
    {
        return ConfigurationValue(ConfigurationName(consumer), key);
    }

    static public string[] TryConfigurationKeys(this IConfigurationConsumer consumer, string path)
    {
        try
        {
            return ConfigurationKeys(consumer, path);
        }
        catch (ArgumentException)
        {
            return new string[0];
        }
    }

    static public string[] ConfigurationKeys(this IConfigurationConsumer consumer, string path)
    {
        return new XmlAppConfiguration(ConfigurationName(consumer) + ".config").GetKeys(path);
    }

    static public string TryConfigurationValue(this IConfigurationConsumer consumer, string key)
    {
        try
        {
            return ConfigurationValue(consumer, key);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    static public string ConfigurationFirstValue(this IConfigurationConsumer consumer, string[] keys)
    {
        foreach (var key in keys)
        {
            var val = TryConfigurationValue(consumer, key);
            if (val != null)
            {
                return val;
            }
        }

        throw new ArgumentException("Unknown key: " + string.Join(",", keys));
    }

    static public string TryConfigurationFirstValue(this IConfigurationConsumer consumer, string[] keys, string defaultValue = null)
    {
        try
        {
            return ConfigurationFirstValue(consumer, keys);
        }
        catch (ArgumentException)
        {
            return defaultValue;
        }
    }

    #region Helper

    static private string ConfigurationValue(string configName, string key)
    {
        return new XmlAppConfiguration(configName + ".config")[key];
    }

    static private string ConfigurationName(object instance)
    {
        var attribute = instance.GetType().GetCustomAttribute<ConfigurationNameAttribute>();
        if (attribute != null)
        {
            return attribute.ConfigName;
        }

        return instance.GetType().ToString().Split('.').Last();
    }

    #endregion
}
