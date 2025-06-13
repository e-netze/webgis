using E.Standard.Extensions.IO;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using System.IO;

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

    static public string AsOutputFilename(this IMap map, string filename)
    {
        var outputPath = map.Environment.UserString(webgisConst.OutputPath);

        if (outputPath.HasHttpUrlSchema())
        {
            if (outputPath.EndsWith("/"))
            {
                return $"{outputPath}{filename}";
            }

            return $"{outputPath.TrimEnd('/')}/{filename}";
        }
        
        return Path.Combine(outputPath, filename);
    }

    static public string AsOutputUrl(this IMap map, string filename)
    {
        var outputUrl = map.Environment.UserString(webgisConst.OutputUrl);

        if (outputUrl.EndsWith("/"))
        {
            return $"{outputUrl}{filename}";
        }

        return $"{outputUrl}/{filename}";
    }
}
