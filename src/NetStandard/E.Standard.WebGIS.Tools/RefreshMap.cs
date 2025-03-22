using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class RefreshMap : IApiClientButton
{
    #region IApiButton Member

    public string Name => "Refresh Map";

    public string Container => "Map";

    public string Image => "refresh.png";

    public string ToolTip => "Redraw map (update all services)";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.refresh;

    #endregion
}
