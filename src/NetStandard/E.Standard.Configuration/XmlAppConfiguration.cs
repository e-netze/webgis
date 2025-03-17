using System;
using System.Collections.Generic;
using System.Xml;

namespace E.Standard.Configuration;

public class XmlAppConfiguration : AppConfiguration
{
    public XmlAppConfiguration(string configFile = "")
        : base(configFile)
    {
        this.ConfigXml = new XmlDocument();
        this.ConfigXml.Load(_configFile);
    }

    private XmlDocument ConfigXml { get; set; }

    public string this[string key]
    {
        get
        {
            XmlNode keyNode = ConfigXml.SelectSingleNode(GetParameterXPath(key) + "/add[@key='" + GetParameterKey(key) + "' and @value]");
            if (keyNode == null)
            {
                throw new ArgumentException("Unknown key: " + key);
            }

            string val = keyNode.Attributes["value"].Value;
            //return Crypto.DecryptConfigSettingsValue(val);
            return val;
        }
    }

    public string TryGetValue(string key)
    {
        try
        {
            return this[key];
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public string ConfigFirstValue(string[] keys)
    {
        foreach (var key in keys)
        {
            var val = TryGetValue(key);
            if (val != null)
            {
                return val;
            }
        }

        throw new ArgumentException("Unknown key: " + string.Join(",", keys));
    }

    public XmlNode AccessControlNode()
    {
        return ConfigXml.SelectSingleNode("configuration/acl");
    }

    public string[] Keys
    {
        get
        {
            List<string> ret = new List<string>();
            foreach (XmlNode node in ConfigXml.SelectNodes("configuration/settings/add[@key and @value]"))
            {
                ret.Add(node.Attributes["key"].Value);
            }

            return ret.ToArray();
        }
    }

    public string[] GetKeys(string path)
    {
        List<string> ret = new List<string>();
        foreach (XmlNode node in ConfigXml.SelectNodes("configuration/settings/" + path + "/add[@key and @value]"))
        {
            ret.Add(node.Attributes["key"].Value);
        }

        return ret.ToArray();
    }

    #region Static Members

    static public XmlAppConfiguration TryLoad(string configFile)
    {
        try
        {
            return new XmlAppConfiguration(configFile);
        }
        catch { }

        return null;
    }

    #endregion

    #region Helper

    private string GetParameterXPath(string key)
    {
        string xPath = String.Empty;
        if (key.Contains(":"))
        {
            xPath = "/" + key.Substring(0, key.LastIndexOf(":")).Trim();
            while (xPath.EndsWith("/"))
            {
                xPath = xPath.Substring(0, xPath.Length - 1);
            }
        }
        return "configuration/settings" + xPath;
    }

    public string GetParameterKey(string key)
    {
        if (key.Contains(":"))
        {
            return key.Substring(key.LastIndexOf(":") + 1);
        }
        return key;
    }

    #endregion
}
