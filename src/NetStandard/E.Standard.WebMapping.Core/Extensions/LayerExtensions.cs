using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.Core.Extensions;

static public class LayerExtensions
{
    static public bool InScale(this ILayer layer, double scale)
    {
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
