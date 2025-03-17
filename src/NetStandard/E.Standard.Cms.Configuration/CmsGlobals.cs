using E.Standard.WebGIS.Core;
using System;

namespace E.Standard.Cms.Configuration;

static public class CmsGlobals
{
    public static Version Version => WebGISVersion.Version;

    public static string JsVersion
    {
        get { return Version.ToString(); }
    }

    public static string CssVersion
    {
        get { return Version.ToString(); }
    }

    static public IFormatProvider Nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;

    static public string SchemaName = "webgis";
}
