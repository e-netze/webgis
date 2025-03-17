using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class LayerBridgeExtensions
{
    static public bool InScale(this ILayerBridge layer, double scale)
    {
        if (layer == null)
        {
            return false;
        }

        if (scale <= 0.0)
        {
            return true;
        }

        if (layer.MinScale > 0 && layer.MinScale > scale)
        {
            return false;
        }

        if (layer.MaxScale > 0 && layer.MaxScale < scale)
        {
            return false;
        }

        return true;
    }
}
