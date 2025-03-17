using gView.GraphicsEngine;
using System.Globalization;
using System.Runtime.InteropServices;

namespace E.Standard.Platform;

public class SystemInfo
{
    static public NumberFormatInfo Nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
    static public NumberFormatInfo Cnf = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;

    static public bool IsLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    static public bool IsWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    static public bool IsOSX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    static public string Platform
    {
        get
        {
            if (IsLinux)
            {
                return OSPlatform.Linux.ToString();
            }

            if (IsOSX)
            {
                return OSPlatform.OSX.ToString();
            }

            if (IsWindows)
            {
                return OSPlatform.Windows.ToString();
            }

            return "Unknown";
        }
    }

    private static string _defaultFontName = null;
    static public string DefaultFontName
    {
        get
        {
            if (_defaultFontName == null)
            {
                _defaultFontName = Current.Engine.GetDefaultFontName();
            }
            return _defaultFontName;
        }
        set { _defaultFontName = value; }
    }

    static public float FontSizeFactor
    {
        get
        {
            if (IsLinux)
            {
                return .45f;
            }

            return 0.92f;
        }
    }
}
