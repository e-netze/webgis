using System;

namespace E.Standard.WebGIS.Core;

public class WebGISVersion
{
    public static Version Version
    {
        get { return new Version(7, 25, 1201); }
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
