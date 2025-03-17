using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class GeoRssServiceControl : NameUrlUserConrol, IInitParameter
{
    private NameUrlControl _nameUrlControl = new NameUrlControl();
    private AuthentificationControl _authentificationControl = new AuthentificationControl();
    private GeoRssService _service = null;
    private GroupBox gbService = new GroupBox() { Label = "GeoRSS Dienst" };
    private Input _textServiceUrl = new Input("textServiceUrl") { Label = "Service/Url" };

    public GeoRssServiceControl()
    {
        this.AddControl(_nameUrlControl);

        this.AddControl(_authentificationControl);

        this.AddControl(gbService);
        gbService.AddControl(_textServiceUrl);
    }

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;

    #region IInitParameter Member

    public object InitParameter
    {
        set
        {
            _service = value as GeoRssService;

            if (_service != null)
            {
                _nameUrlControl.InitParameter = _service;
                _authentificationControl.Authentification = _service;

                _textServiceUrl.Value = _service.ServiceUrl;
            }
        }
    }

    #endregion
}
