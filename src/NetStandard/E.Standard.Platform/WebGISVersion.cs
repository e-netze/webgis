using System;

namespace E.Standard.Platform;

public class WebGISVersion
{
    private static Version _version = new Version(8, 26, 1301);
    private static string _versionString = _version.ToString();

    public static Version Version => _version;

    public static string JsVersion
    {
        get => _versionString;
    }

    public static string CssVersion
    {
        get => _versionString;
    }
}
