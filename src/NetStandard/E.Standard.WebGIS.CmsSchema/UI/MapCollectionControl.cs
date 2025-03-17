using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class MapCollectionControl : UserControl, IInitParameter
{
    public MapCollectionControl()
    {
        this.AddControl(_nameUrlControl);
    }

    private NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl");

    public object InitParameter
    {
        set
        {
            var mapCollection = value as MapCollection;

            if (mapCollection != null)
            {
                _nameUrlControl.InitParameter = value;
            }
        }
    }
}
