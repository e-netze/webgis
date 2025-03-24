using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools.MapMarkup;

[Export(typeof(IApiButton))]
[ToolClient("mapbuilder")]
public class MapMarkupBuilder : MapMarkup
{
    public MapMarkupBuilder()
    {
        base.toolContainerId = base.toolContainerId + "_mapbuilder";
    }

    public override string Name => "Map-Markup";
}
