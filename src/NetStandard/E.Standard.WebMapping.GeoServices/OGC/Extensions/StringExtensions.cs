using E.Standard.WebGIS.CMS;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.OGC.Extensions;

static internal class StringExtensions
{
    static public string AppendUrlParameters(this string url, string parameter)
    {
        while (url.EndsWith("?") || url.EndsWith("&"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        if (url.Contains("?"))
        {
            return $"{url}&{parameter}";
        }

        return $"{url}?{parameter}";
    }

    static public string ToFileExtenstion(this string imageFormat)
    {
        switch (imageFormat)
        {
            case "jpg":
            case "jpeg":
                return "jpg";
            case "png":
            case "png8":
            case "png24":
            case "png32":
                return "png";
            default:
                return imageFormat;
        }
    }

    static public bool IsEqualEPSG(this string epsg1, string epsg2)
    {
        // values that can come in: (same, same)
        // http://www.opengis.net/gml/srs/epsg.xml#31256
        // EPSG:31256
        // 31256

        if (string.IsNullOrEmpty(epsg1) || string.IsNullOrEmpty(epsg2))
        {
            return true;
        }

        try
        {
            var code1 = int.Parse(epsg1.Replace('#', ':').Split(':').Last());
            var code2 = int.Parse(epsg1.Replace('#', ':').Split(':').Last());

            return code1 == code2;
        }
        catch
        {
            return false;
        }
    }

    static public StringBuilder AppendSldVersion(this StringBuilder sb, SLD_Version sldVersion)
    {
        switch (sldVersion)
        {
            case SLD_Version.version_1_0_0:
                sb.Append("&SLD_VERSION=1.0.0");
                break;
            case SLD_Version.version_1_1_0:
                sb.Append("&SLD_VERSION=1.1.0");
                break;
        }

        return sb;
    }
}
