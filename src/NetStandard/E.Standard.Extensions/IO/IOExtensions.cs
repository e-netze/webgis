using System;

namespace E.Standard.Extensions.IO;

static public class IOExtensions
{
    static public bool IsValidHttpUrl(this string s)
    {
        if (!String.IsNullOrEmpty(s))
        {
            if (!s.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) &&
                !s.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if (Uri.TryCreate(s, UriKind.Absolute, out Uri resultURI))
            {
                return (resultURI.Scheme == Uri.UriSchemeHttp ||
                        resultURI.Scheme == Uri.UriSchemeHttps);
            }
        }
        return false;
    }
}
