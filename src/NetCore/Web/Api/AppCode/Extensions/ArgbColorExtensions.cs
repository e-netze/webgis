using gView.GraphicsEngine;

namespace Api.Core.AppCode.Extensions;

public static class ArgbColorExtensions
{
    public static string ToHexString(this ArgbColor color, bool withAlpha = true)
    {
        if (withAlpha && color.A <= 255)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }
        else
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }
}
