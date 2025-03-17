using E.Standard.Api.App.DTOs;
using System.IO;
using System.Xml;

namespace E.Standard.Api.App.Extensions;

static public class EditThemeExtensions
{
    static public XmlNode EditThemeXmlNode(this EditThemeDTO editTheme, string etcPath)
    {
        DirectoryInfo di = new DirectoryInfo(etcPath + @"/editing/themes");
        foreach (FileInfo fi in di.GetFiles("*.xml"))
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fi.FullName);
                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("webgis", "http://www.e-steiermark.com/webgis");
                ns.AddNamespace("edit", "http://www.e-steiermark.com/webgis/edit");

                XmlNode node = doc.SelectSingleNode("editthemes/edit:edittheme[@id='" + editTheme.ThemeId + "']", ns);
                if (node != null)
                {
                    return node;
                }
            }
            catch { }
        }

        return null;
    }
}
