using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class FormGeneralTileCache : NameUrlUserConrol, IInitParameter
{
    private NameUrlControl _nameUrlControl = new NameUrlControl();
    private ComboBox _cmbTemplate = new ComboBox("cmbTemplate") { Label = "Vorlage" };
    private GeneralTileCache _cache = null;

    public FormGeneralTileCache()
    {
        this.AddControl(_nameUrlControl);
        this.AddControl(_cmbTemplate);

        foreach (GeneralTileCache.CreateTemplate template in Enum.GetValues(typeof(GeneralTileCache.CreateTemplate)))
        {
            _cmbTemplate.Options.Add(new ComboBox.Option(template.ToString()));
        }
    }

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;

    #region IInitParameter Member

    public object InitParameter
    {
        set
        {
            _nameUrlControl.InitParameter = value;

            _cache = value as GeneralTileCache;
        }
    }

    #endregion
}
