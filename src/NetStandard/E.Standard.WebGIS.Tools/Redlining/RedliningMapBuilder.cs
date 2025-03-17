using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools.Redlining;

[Export(typeof(IApiButton))]
[ToolClient("mapbuilder")]
public class RedliningMapBuilder : Redlining
{
    public RedliningMapBuilder()
    {
        base.toolContainerId = base.toolContainerId + "_mapbuilder";
    }

    public override string Name => "Redlining";
}
