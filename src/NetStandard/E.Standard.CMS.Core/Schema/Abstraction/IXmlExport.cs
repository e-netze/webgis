using System.Xml;

using E.Standard.CMS.Core.UI.Abstraction;

namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface IXmlExport
{
    bool Export(ICmsApplicationSettings settings, XmlDocument xml);
}
