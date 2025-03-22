using E.Standard.Localization.Reflection;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[LocalizationNamespace("tools.zoomback")]
public class ZoomBack : IApiClientButton
{
    #region IApiButton Member

    public string Name => "Back";

    public string Container => "Navigation";

    public string Image => "back.png";

    public string ToolTip => "Zoom to previous extent";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.back;

    #endregion
}
