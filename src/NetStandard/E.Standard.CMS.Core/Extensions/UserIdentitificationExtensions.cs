using System;

namespace E.Standard.CMS.Core.Extensions;

static public class UserIdentitificationExtensions
{
    static public string RemoveUserIdentificationNamespace(this string username)
    {
        if (username != null && username.Contains("::"))
        {
            username = username.Substring(username.IndexOf("::") + 2);
        }
        return username;
    }

    static public string RemoveUserIdentificationDomain(this string username)
    {
        if (username != null && username.Contains(@"\"))
        {
            username = username.Substring(username.IndexOf(@"\") + 1);
        }
        return username;
    }

    static public string UsernameDomain(this string username)
    {
        if (username != null)
        {
            if (username.Contains("@"))
            {
                return username.Substring(username.IndexOf("@") + 1).ToLower();
            }

            if (username.Contains("\\"))
            {
                return username.Substring(0, username.IndexOf("\\"));
            }
        }

        return String.Empty;
    }
}
