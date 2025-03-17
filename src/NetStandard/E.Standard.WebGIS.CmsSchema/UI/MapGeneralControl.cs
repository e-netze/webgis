using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class MapGeneralControl : UserControl, IInitParameter
{
    public MapGeneralControl()
    {
        this.AddControl(_nameUrlControl);
    }

    private NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl");

    public object InitParameter
    {
        set
        {
            var map = value as MapGeneral;

            if (map != null)
            {
                _nameUrlControl.InitParameter = value;
            }
        }
    }
}
