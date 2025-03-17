using E.Standard.CMS.Core;

namespace E.Standard.WebGIS.Core;

static public class WebGISHelper
{
    static public string ReplacePlaceholders(this string str, CmsDocument.UserIdentification ui)
    {
        if (ui != null && ui.Username != null)
        {
            string username = ui.Username.RawUsername();

            str = str.Replace("{{username}}", username);
        }

        return str;
    }

    static public string RawUsername(this string username)
    {
        return username.Contains("::") ? username.Substring(username.IndexOf("::") + 2) : username;
    }
}
