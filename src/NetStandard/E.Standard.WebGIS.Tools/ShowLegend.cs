using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class ShowLegend : IApiClientButton
{
    #region IApiButton Member

    public string Name => "Legende und Themen";

    public string Container => "Karte";

    public string Image => "legend.png";

    public string ToolTip => "Legende anzeigen";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.showlegend;

    #endregion
}

[Export(typeof(IApiButton))]
[ToolClient("mapbuilder")]
public class ShowLegendMapBuilder : ShowLegend
{

}
