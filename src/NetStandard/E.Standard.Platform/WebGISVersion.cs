using System;

namespace E.Standard.Platform;

public class WebGISVersion
{
    public static Version Version
    {
        get { return new Version(8, 26, 301); }
    }

    public static string JsVersion
    {
        get { return Version.ToString(); }
    }

    public static string CssVersion
    {
        get { return Version.ToString(); }
    }
}
