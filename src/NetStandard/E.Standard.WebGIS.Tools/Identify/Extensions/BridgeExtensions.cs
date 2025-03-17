using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.Tools.Identify.Extensions;

static internal class BridgeExtensions
{
    public static string IdentifyCustomSelection(this IBridge bridge, Shape queryShape, SpatialReference queryShapeSref, ApiToolEventArguments e)
    {
        if ((queryShape is Polygon || queryShape is Polyline)
            && e.AsDefaultTool == false
            /* && e[IdentifyDefault.SketchCanApplyBufferId] == "true"*/)
        {
            //var bufferDistance = e.GetDouble(IdentifyDefault.SketchBufferDistanceId);
            //if (!double.IsNaN(bufferDistance) && bufferDistance != 0.0)
            {
                if (queryShapeSref != null)
                {
                    return bridge.CreateCustomSelection(queryShape, queryShapeSref);
                }
            }
        }

        return string.Empty;
    }
}
