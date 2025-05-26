using System;
using System.Text;

namespace E.Standard.WebMapping.Core.Extensions;

static public class UriExtensions
{
    static public string FileTitleFromUriString(this string uri)
    {
        uri = uri.Replace("\\", "/").Replace("//", "/");

        int index = 0;
        while ((index = uri.IndexOf("/")) > -1)
        {
            uri = uri.Substring(index + 1, uri.Length - (index + 1));
        }

        uri = uri.Replace("&", "_").Replace("?", "_").Replace(":", "_");
        if (uri.Length > 150)
        {
            int pos = uri.LastIndexOf(".");
            string ext = uri.Substring(pos, uri.Length - pos);
            uri = "ims_" + System.Guid.NewGuid().ToString("N") + ext;
        }
        return uri;
    }

    static public string FileTitle(this Uri uri)
    {
        return uri.ToString().FileTitleFromUriString();
    }
}
