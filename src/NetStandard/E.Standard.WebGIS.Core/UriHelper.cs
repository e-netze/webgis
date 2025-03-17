namespace E.Standard.WebGIS.Core;

static public class UrlExtensions
{
    static public string WithoutEndingSlashes(this string url)
    {
        url = url.Trim();

        while (url.EndsWith("/"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        return url;
    }

    static public string WithoutEndingSlashesAndBackSlasches(this string url)
    {
        url = url.Trim();

        while (url.EndsWith("/"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        while (url.EndsWith("\\"))
        {
            url = url.Substring(0, url.Length - 1);
        }

        return url;
    }
}
