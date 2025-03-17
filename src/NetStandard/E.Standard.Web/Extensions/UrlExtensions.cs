using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace E.Standard.Web.Extensions;

static public class UrlExtensions
{
    static public string ReplaceUrlHeaderPlaceholders(
                this string url,
                NameValueCollection? headers,
                Func<string, string> defaultValue
        )
    {
        if (headers is null
            || headers.Count == 0
            || String.IsNullOrEmpty(url)
            || !(url.Contains("{") && url.Contains("}"))
            )
        {
            return url;
        }

        foreach (var placeholder in url.UrlPlaceholders())
        {
            url = url.Replace($"{{{placeholder}}}",
                               placeholder.UrlPlaceholderValueFromCollection(headers, "header", defaultValue(placeholder)));
        }

        return url;
    }

    static public string ReplaceUrlHeaderPlaceholders(
                this string url,
                NameValueCollection? headers
            )
        => url.ReplaceUrlHeaderPlaceholders(headers, value => $"{{{value}}}");

    static public string ReplaceUrlPlaceholders(
                this string url,
                HttpRequest? request,
                Func<string, string> defaultValue
            )
        => url.ReplaceUrlHeaderPlaceholders(request?.HeadersCollection(), defaultValue);

    #region Helpers

    static private IEnumerable<string> UrlPlaceholders(this string commandLine, string startBracket = "{", string endBracket = "}")
    {
        int pos1 = 0, pos2;
        pos1 = commandLine.IndexOf(startBracket);
        List<string> parameters = new List<string>();

        while (pos1 != -1)
        {
            pos2 = commandLine.IndexOf(endBracket, pos1);
            if (pos2 == -1)
            {
                break;
            }

            parameters.Add(commandLine.Substring(pos1 + startBracket.Length, pos2 - pos1 - 1));
            pos1 = commandLine.IndexOf(startBracket, pos2);
        }

        return parameters;
    }

    static private string UrlPlaceholderValueFromCollection(this string key, NameValueCollection nvc, string keyNamespace, string defaultValue)
    {
        if (!String.IsNullOrWhiteSpace(keyNamespace))
        {
            if (!key.ToLower().StartsWith($"{keyNamespace.ToLower()}:"))
            {
                return key;
            }

            key = key.Substring(keyNamespace.Length + 1).Trim();
        }

        string subParts = String.Empty;
        if (key.Contains("/"))
        {
            //
            //  Wird bei PVP verwendet. Hier kommt ein Parameter X-Orig-Uri mit /application/root/gem123/at.gv.ooe.intramap/webgis5/Content/intramap/Kataster
            //  daher mit {X-Orig-Uri/webgis5} kann man so angeben, dass die Url nur bis zu diesem Pfad ausgelesen wird (ohne /webgis5)
            //  Erweiterung: mehrer SubParts {X-Orig-Uri/webgis5|/api5} mit | getrennt
            subParts = key.Substring(key.IndexOf("/"));
            key = key.Substring(0, key.IndexOf("/"));
        }
        string val = ((nvc != null ? nvc[key] : String.Empty) ?? defaultValue) ?? String.Empty;

        if (!String.IsNullOrWhiteSpace(subParts))
        {
            foreach (var sub in subParts.Split('|'))
            {
                int pos = val.ToLower().IndexOf(sub.ToLower());
                if (pos >= 0)
                {
                    val = val.Substring(0, pos);
                    break;
                }
            }
        }

        return val;
    }

    #endregion
}
