using E.Standard.Api.App.Extensions;
using E.Standard.Configuration.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Api.Core.AppCode.Extensions;

static public class StringExtensions
{
    static public void CheckAllowedCustomServiceUrl(this string url, HttpRequest request, ConfigurationService config)
    {
        Uri.TryCreate(url, UriKind.Absolute, out Uri uri);

        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new Exception("Invalid Url");
        }

        if ((uri.Host == "localhost" || uri.Host.StartsWith("127.0")) &&
            !"localhost".Equals(new Uri(request.GetDisplayUrl()).Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Forbidden (local) host");
        }

        if (uri.Host != "localhost")
        {
            if (uri.Host
                .Split('.')
                .Select(s => s.Trim())
                .Where(s => !String.IsNullOrEmpty(s))
                .Count() < 2)
            {
                throw new Exception("Malformed host. Example: www.server.com");
            }
        }

        // localhost,^127\.0\..*,.*\.domain\.at$,^192\.10\..*
        foreach (var blackList in config.SecurityAddCustomServiceHostBlacklist())
        {
            Regex regex = new Regex(blackList);
            if (regex.IsMatch(uri.Host) || regex.IsMatch(uri.Host.ToLower()) || blackList == "forbidden")  // if blacklist contains "forbidden" als servers are forbidden, no custom Services are allowed!! => Internet Atlas 
            {
                throw new Exception("Forbidden host");
            }
        }
    }

    static public T[] UrlParameterToArray<T>(this string str, char separator = ',')
    {
        if (String.IsNullOrEmpty(str) || "null".Equals(str, StringComparison.OrdinalIgnoreCase))
        {
            return new T[0];
        }

        return str.Split(separator).Select(s => 
                typeof(T) switch
                {
                    Type t when t == typeof(short) => (T)(object)short.Parse(s.Trim()),
                    Type t when t == typeof(int) => (T)(object)int.Parse(s.Trim()),
                    Type t when t == typeof(long) => (T)(object)long.Parse(s.Trim()),

                    Type t when t == typeof(string) => (T)(object)s.Trim(),
                    
                    Type t when t == typeof(float) => (T)(object)float.Parse(s.Trim()),
                    Type t when t == typeof(double) => (T)(object)double.Parse(s.Trim()),

                    Type t when t == typeof(bool) => (T)(object)bool.Parse(s.Trim()),
                    _ => (T)Convert.ChangeType(s.Trim(), typeof(T)),
                }
            ).ToArray();
    }
}
