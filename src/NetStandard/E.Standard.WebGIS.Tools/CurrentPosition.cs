using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class CurrentPosition : IApiClientButton
{
    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.currentpos;

    #endregion

    #region IApiButton Member

    public string Name => "Current Position";

    public string Container => "Navigation";

    public string Image => "currentpos.png";

    public string ToolTip => "Zoom to current (GPS) position";

    public bool HasUI => false;

    #endregion
}
