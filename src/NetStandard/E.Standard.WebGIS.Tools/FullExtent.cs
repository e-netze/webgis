using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class FullExtent : IApiClientButton
{
    #region IApiButton Member

    public string Name => "Gesamter Kartenauschnitt";

    public string Container => "Navigation";

    public string Image => "home.png";

    public string ToolTip => "auf maximalen Extent zoomen";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.fullextent;

    #endregion
}
