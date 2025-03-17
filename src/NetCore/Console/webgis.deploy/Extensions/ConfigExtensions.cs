using System.Xml.Linq;

namespace webgis.deploy.Extensions;
static internal class ConfigExtensions
{
    static public string GetXmlConfigValue(this string configFilePath, string key)
    {
        XDocument doc = XDocument.Load(configFilePath);

        var element = doc.Descendants("add")
                        .FirstOrDefault(el => el.Attribute("key")?.Value == key);

        return element?.Attribute("value")?.Value;
    }
}
