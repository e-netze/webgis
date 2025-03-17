using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Reflection;

namespace E.Standard.WebGIS.Tools.Editing.Mobile;

[Export(typeof(IApiButton))]
[ToolId("webgis.tools.editing.editpolylinefeature")]
class EditPolylineFeature : UpdateFeature
{
    public override ToolType Type
    {
        get
        {
            return ToolType.sketch1d;
        }
    }
}
