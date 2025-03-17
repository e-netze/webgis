using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;

namespace E.Standard.WebMapping.GeoServices.ArcServer;

class ASHelper
{
    public static void CleanSelectionBitmap(IBitmap bm, ArgbColor selColor)
    {
        int w = bm.Width;
        int h = bm.Height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var c = bm.GetPixel(x, y);

                if (c.R == selColor.R &&
                    c.G == selColor.G &&
                    c.B == selColor.B)
                {
                }
                else
                {
                    bm.SetPixel(x, y, ArgbColor.Transparent);
                }
            }
        }
    }
}
