using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class TocGroupControl : NameUrlUserConrol, IInitParameter
{
    //private TocGroup _group = null;
    private NameUrlControl _nameUrlControl;

    public TocGroupControl()
    {
        this._nameUrlControl = new NameUrlControl()
        {
            UrlIsVisible = false
        };

        this.AddControl(_nameUrlControl);
    }

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;

    #region IInitParameter Member

    public object InitParameter
    {
        set { _nameUrlControl.InitParameter = value; }
    }

    #endregion
}
