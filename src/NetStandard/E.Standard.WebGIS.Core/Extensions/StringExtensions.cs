using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Core.Extensions;

static public class StringExtensions
{
    static public string Username2StorageDirectory(this string username)
    {
        return username.Replace(":", "~").Replace(@"\", "$");
    }

    static public string StorageDirectory2Username(this string directoryName)
    {
        return directoryName.Replace("~", ":").Replace("$", @"\");
    }

    static public string RemoveAuthPrefix(this string authName)
    {
        if (authName != null && authName.Contains("::"))
        {
            return authName.Substring(authName.IndexOf("::") + 2);
        }

        return authName;
    }

    static public IEnumerable<string> RemoveAuthPrfix(this IEnumerable<string> authNames)
    {
        if (authNames == null)
        {
            return null;
        }

        return authNames.Select(a => a.RemoveAuthPrefix());
    }
}
