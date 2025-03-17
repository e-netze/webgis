using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;

namespace E.Standard.WebGIS.CmsSchema;

public class CmsUpload : IXmlExport
{
    #region IXmlExport Member

    public bool Export(ICmsApplicationSettings settings, System.Xml.XmlDocument xml)
    {
        //FormExport dlg = new FormExport(settings, xml);
        //if (dlg.ShowDialog() == DialogResult.OK)
        //{
        //    return true;
        //}
        return false;
    }

    #endregion
}
