using System;

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
