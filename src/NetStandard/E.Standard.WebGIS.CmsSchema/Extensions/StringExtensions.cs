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
}
