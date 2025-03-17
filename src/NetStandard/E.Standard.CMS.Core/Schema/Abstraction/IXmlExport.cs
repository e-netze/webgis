using E.Standard.CMS.Core.UI.Abstraction;
using System.Xml;

namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface IXmlExport
{
    bool Export(ICmsApplicationSettings settings, XmlDocument xml);
}
