using E.Standard.Custom.Core.Abstractions;
using E.Standard.Json;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Mvc.Wrapper;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace Portal.Core.AppCode.Extensions;

static public class GeneralExtensions
{
    static public IHttpRequestWrapper Wrapper(this HttpRequest request)
    {
        return null;
    }

    static public IPortalAuthenticationService GetService(this IEnumerable<IPortalAuthenticationService> authenticationServices, UserType userType)
    {
        return authenticationServices?
                        .Where(a => a.UserType == userType)
                        .FirstOrDefault();
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

    static public bool IsNonePortalId(this string portalId)
    {
        return Const.NonePortalId.Equals(portalId);
    }

    static public string ToProtectedEmail(this string emailAddress)
    {
        if (emailAddress.Contains("@"))
        {
            emailAddress = emailAddress.Split('@')[0].Substring(0, 2).PadRight(emailAddress.Split('@')[0].Length, '*') + "@" + emailAddress.Split('@')[1];
        }

        return emailAddress;
    }

    static public string RemoveEndingSlashes(this string str)
    {
        while (str.EndsWith("/"))
        {
            str = str.Substring(0, str.Length - 1);
        }

        return str;
    }

    static public string ToJavaScriptEncodedString(this string str)
    {
        str = JSerializer.Serialize(str);
        return str.Substring(1, str.Length - 2);  // remove beginning and ending quotes
    }
}
