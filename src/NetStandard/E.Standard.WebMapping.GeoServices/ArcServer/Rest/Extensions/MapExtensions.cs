using E.Standard.WebMapping.Core.Abstraction;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
static internal class MapExtensions
{
    static public (int imageWidth, int imageHeight, double dpi, bool modified) CalcImageSizeAndDpi(this IMap map, int maxImageWidth, int maxImageHeight)
    {
        if ((maxImageWidth == 0 && maxImageHeight == 0)
            || map.ImageWidth == 0 || map.ImageHeight == 0)
        {
            return (map.ImageWidth, map.ImageHeight, map.Dpi, false);
        }

        bool modified = false;
        int imageWidth = map.ImageWidth, imageHeight = map.ImageHeight;
        double dpi = map.Dpi;
        float ratio = imageWidth / (float)imageHeight;

        if (imageWidth > maxImageWidth)
        {
            float diffRatio = imageWidth / (float)maxImageWidth;

            imageWidth = maxImageWidth;
            imageHeight = (int)(imageWidth / ratio);
            dpi /= diffRatio;
            modified = true;
        }
        if (imageHeight > maxImageHeight)
        {
            float diffRatio = imageHeight / (float)maxImageHeight;

            imageHeight = maxImageHeight;
            imageWidth = (int)(imageHeight * ratio);
            dpi /= diffRatio;
            modified = true;
        }

        return (imageWidth, imageHeight, dpi, modified);
    }
}
