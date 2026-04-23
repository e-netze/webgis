using System;

namespace E.Standard.Platform;

public class WebGISVersion
{
    private static Version _version = new Version(8, 26, 1703);
    private static string _versionString = _version.ToString();

    public static Version Version => _version;

    public static string JsVersion => _versionString;
    
    public static string CssVersion => _versionString;
}
