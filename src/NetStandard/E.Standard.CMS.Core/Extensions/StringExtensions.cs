using System;
using System.Text;

namespace E.Standard.CMS.Core.Extensions;
static internal class StringExtensions
{
    static public string ToValidNodeUrl(this string url)
    {
        if (String.IsNullOrEmpty(url))
        {
            return url;
        }

        url = url.Replace(" ", "_");

        foreach (char c in new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' })
        {
            if (url.StartsWith(c))
            {
                url = $"_{url}";
            }
        }

        return url;
    }
}

static public class  PublicStringExtensions
{
    static public string TrimRightRelativeCmsPath(this string path, int trim)
    {
        string[] parts = path.Replace(@"\", "/").Split('/');
        if (parts.Length <= trim)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < parts.Length - trim; i++)
        {
            if (i > 0)
            {
                sb.Append("/");
            }

            sb.Append(parts[i]);
        }

        return sb.ToString();
    }

    static public string TrimLeftRelativeCmsPath(this string path, int trim)
    {
        string[] parts = path.Replace(@"\", "/").Split('/');
        if (parts.Length <= trim)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = parts.Length - trim; i < parts.Length; i++)
        {
            if (sb.Length > 0)
            {
                sb.Append("/");
            }

            sb.Append(parts[i]);
        }

        return sb.ToString();
    }
}
