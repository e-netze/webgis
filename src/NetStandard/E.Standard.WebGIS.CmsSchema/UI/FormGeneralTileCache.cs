using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class FormGeneralVectorTileCache : NameUrlUserConrol, IInitParameter
{
    private NameUrlControl _nameUrlControl = new NameUrlControl();
    private GeneralVectorTileCache _cache = null;

    public FormGeneralVectorTileCache()
    {
        this.AddControl(_nameUrlControl);
    }

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;

    #region IInitParameter Member

    public object InitParameter
    {
        set
        {
            _nameUrlControl.InitParameter = value;

            _cache = value as GeneralVectorTileCache;
        }
    }

    #endregion
}
