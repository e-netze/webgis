using System;
using System.IO;
using System.Reflection;

namespace E.Standard.Configuration;

public class AppConfiguration
{
    protected string _configFile;

    public AppConfiguration(string configFile)
    {
        if (!configFile.Replace(@"\", "/").Contains(@"/"))
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var fi1 = new ConfigFileInfo(Path.Combine(rootPath ?? "", "_config", configFile));  // rootpath can be null (linux)
            var fi2 = new ConfigFileInfo($"{Assembly.GetEntryAssembly().Location}.__config");

            if (fi1.Exists)
            {
                _configFile = fi1.FullName;
            }
            else if (fi2.Exists)
            {
                _configFile = fi2.FullName;
            }
        }
        else
        {
            _configFile = configFile;
        }
    }

    public bool Exists
    {
        get
        {
            if (String.IsNullOrWhiteSpace(_configFile))
            {
                return false;
            }

            FileInfo fi = new FileInfo(_configFile);
            return fi.Exists;
        }
    }

    public string ConfigurationFile
    {
        get { return _configFile; }
    }
}
