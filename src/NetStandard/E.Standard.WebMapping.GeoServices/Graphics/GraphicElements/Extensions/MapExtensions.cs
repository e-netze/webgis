using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements.Extensions;

static public class MapExtensions
{
    static public float DpiFactor(this IMap map)
    {
        if (map != null && map.Dpi > 0)
        {
            return (float)map.Dpi / 96f;
        }

        return 1;
    }
}
