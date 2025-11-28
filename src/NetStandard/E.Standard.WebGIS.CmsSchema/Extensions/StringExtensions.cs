using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.Platform;
using System;

namespace E.Standard.WebGIS.CmsSchema.Extensions;
internal static class StringExtensions
{
    //public static string UrlEncodePassword(this string password)
    //{
    //    if (password != null && password.IndexOfAny("+/=&".ToCharArray()) >= 0)
    //    {
    //        password = System.Web.HttpUtility.UrlEncode(password);
    //    }

    //    return password;
    //}

    private static readonly char[] UrlEncodeChars = { '+', '/', '=', '&' };

    public static string UrlEncodePassword(this string password)
    {
        if (string.IsNullOrEmpty(password))
            return password;

        // Use Span for efficient search
        if (password.AsSpan().IndexOfAny(UrlEncodeChars) >= 0)
        {
            password = System.Web.HttpUtility.UrlEncode(password);
        }

        return password;
    }

    static public string TrimAndAppendSchemaNodePath(this string relativePath, int trim, string schemaNodePath)
    {
        string trimedPath = relativePath.TrimRightRelativeCmsPath(trim).TrimEnd('/', '\\');
        if (SystemInfo.IsLinux)
        {
            // in linux schemaNodePath is always lower case
            schemaNodePath = schemaNodePath.ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(trimedPath))
        {
            return schemaNodePath;
        }

        schemaNodePath = schemaNodePath.TrimStart('/', '\\');

        

        return $"{trimedPath}/{schemaNodePath}";
    }
}
