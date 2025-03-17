using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class ZoomBack : IApiClientButton
{
    #region IApiButton Member

    public string Name => "Zurück";

    public string Container => "Navigation";

    public string Image => "back.png";

    public string ToolTip => "Letzter Kartenauschnitt";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.back;

    #endregion
}
