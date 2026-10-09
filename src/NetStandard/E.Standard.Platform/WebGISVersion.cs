using System;

namespace E.Standard.Platform;

public class WebGISVersion
{
    public const int Major = 9;
    public const int Minor = 26;

    private static Version _version = new Version(Major, Minor, 4102);
    private static string _versionString = _version.ToString();

    public static Version Version => _version;

    public static string JsVersion => _versionString;
    
    public static string CssVersion => _versionString;
}
