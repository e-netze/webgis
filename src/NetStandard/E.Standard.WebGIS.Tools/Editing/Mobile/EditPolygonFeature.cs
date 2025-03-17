using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Reflection;

namespace E.Standard.WebGIS.Tools.Editing.Mobile;

[Export(typeof(IApiButton))]
[ToolId("webgis.tools.editing.editpolygonfeature")]
class EditPolygonFeature : UpdateFeature
{
    public override ToolType Type
    {
        get
        {
            return ToolType.sketch2d;
        }
    }
}
