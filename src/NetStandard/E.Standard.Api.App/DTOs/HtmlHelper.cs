using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Mvc.Wrapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class HtmlHelper
{
    public static string ToList(IHtml[] html, string header = "", HeaderType headerType = HeaderType.h2, CacheService cache = null)
    {
        StringBuilder sb = new StringBuilder();

        if (!String.IsNullOrEmpty(header))
        {
            sb.Append(ToHeader(header, headerType));
        }

        if (html != null)
        {
            sb.Append("<ul>");
            foreach (var o in html)
            {
                sb.Append("<li>");
                sb.Append(o is IHtml3 ? ((IHtml3)o).ToHtmlString(cache) : o.ToHtmlString());
                if (o is IHtml2)
                {
                    sb.Append("<div style='padding:10px'>");
                    foreach (string link in ((IHtml2)o).PropertyLinks)
                    {
                        sb.Append(HtmlHelper.ToNextLevelLink(link.ToLower(), link, "display:inline"));
                    }
                    sb.Append("</div>");
                }
                sb.Append("</li>");
            }
            sb.Append("</ul>");
        }

        return sb.ToString();
    }

    public static string ToNextLevelLink(string next, string name, string styles = "", string color = "#", string background = "")
    {
        return $"<a onclick=\"moveDown('{next}');return false;\" href='' style='text-decoration:none;padding:0px;{styles}'><div style='background:{background.OrTake("#b5dbad")};color:{color.OrTake("#333")};border:2px solid #82c828;cursor:pointer;width:98%;padding:5px;margin-top:4px;border-radius:20px'><div class='enter-button'></div><span style='position:relative;top:-7px;left:5px'>" + name + "</span></div></a>";
    }

    public static string ToTable(string[] headers, object[] values)
    {
        StringBuilder sb = new StringBuilder();

        if (headers != null && values != null && headers.Length > 0)
        {
            sb.Append("<div>");
            sb.Append("<div style='cursor:pointer;background:#eee;border:1px solid #aaa;margin-top:2px;padding-left:10px;border-radius:12px' onclick='$(this).parent().find(\".col_div\").slideToggle();'><table style='width:100%;margin-top:0px'>");
            sb.Append("<tr><td><strong>" + headers[0] + "<strong></td>");
            sb.Append("<td style='width:100%'>" + (values.Length > 0 ? values[0] : String.Empty) + "</td>");
            sb.Append("<td>\\/</td>");
            sb.Append("</table></div>");

            sb.Append("<div class='col_div' style='display:none;padding-left:10px'><table style='width:100%;margin-top:0px;'>");
            for (int i = 1; i < headers.Length; i++)
            {
                string header = headers[i];
                object value = values.Length > i ? values[i] : String.Empty;

                sb.Append("<tr><td>");

                sb.Append("<strong>" + header + "</strong>");
                if (value != null)
                {
                    sb.Append("<td style='width:100%'>" + (value is IEnumerable ? EnumerationToString((IEnumerable)value) : value.ToString()) + "</td>");
                }
                sb.Append("</td></tr>");
            }
            sb.Append("</table></div>");
            sb.Append("</div>");
        }

        return sb.ToString();
    }

    public static string ToTable(IDictionary<string, object> dict)
    {
        StringBuilder sb = new StringBuilder();

        if (dict != null)
        {
            sb.Append("<table style='width:100%;border-bottom:1px solid #aaa'>");
            foreach (string header in dict.Keys)
            {
                object value = dict[header];

                sb.Append("<tr><td>");
                sb.Append("<strong>" + header + "</strong>");
                if (value != null)
                {
                    sb.Append("<td style='width:100%'>" + (value is IEnumerable ? EnumerationToString((IEnumerable)value) : value.ToString()) + "</td>");
                }

                sb.Append("</td></tr>");
            }
            sb.Append("</table>");
        }

        return sb.ToString();
    }

    public static string ToTable(IEnumerable<string> headers, IEnumerable<IDictionary<string, object>> rows)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<div>");
        sb.Append("<table>");

        sb.Append("<tr>");
        foreach (var header in headers)
        {
            sb.Append($"<th><strong>{header}</strong></th>");
        }
        sb.Append("</tr>");

        foreach (var row in rows)
        {
            sb.Append("<tr>");
            foreach (var header in headers)
            {
                string val = row.ContainsKey(header) ? row[header]?.ToString() : String.Empty;
                sb.Append($"<td style='white-space:nowrap'>{val}</td>");
            }
            sb.Append("</tr>");
        }

        sb.Append("</table>");
        sb.Append("</div>");

        return sb.ToString();
    }
    public static string EnumerationToString(IEnumerable e)
    {
        if (e is string)
        {
            return e.ToString();
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("<ul style='padding:0px'>");
        foreach (object o in e)
        {
            sb.Append("<li>");
            if (o == null)
            {
                sb.Append("null");
            }
            else
            {
                sb.Append(o.ToString());
            }

            sb.Append("</li>");
        }
        sb.Append("</ul>");
        return sb.ToString();
    }

    public static string ToHeader(string title, HeaderType headerType)
    {
        switch (headerType)
        {
            case HeaderType.h2: return "<h2>" + title + "</h2>";
            case HeaderType.h3: return "<h3>" + title + "</h3>";
            case HeaderType.h4: return "<h4>" + title + "</h4>";
            case HeaderType.h5: return "<h5>" + title + "</h5>";
            case HeaderType.h6: return "<h6>" + title + "</h6>";
        }
        return "<h1>" + title + "</h1>";
    }

    public static string Input(string name, string id)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<span>");
        sb.Append(name);
        sb.Append(":</span><br/>");
        sb.Append("<input name='" + id + "' class='webgis-input' /><br/>");

        return sb.ToString();
    }

    public static string Checkbox(string name, string id)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<label>");
        sb.Append("<input type='checkbox' name='" + id + "' class='webgis-checkbox' />&nbsp;");
        sb.Append(name);
        sb.Append("</label>");

        return sb.ToString();
    }

    public static string Combobox(string name, string id, Dictionary<string, string> options)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<span>");
        sb.Append(name);
        sb.Append(":</span><br/>");
        sb.Append("<select name='" + id + "' class='webgis-input'>");
        foreach (string key in options.Keys)
        {
            sb.Append("<option value='" + key + "'>" + options[key] + "</option>");
        }

        sb.Append("</select><br/>");

        return sb.ToString();
    }

    public static string Autocomplete(string name, string id, string source)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("<span>");
        sb.Append(name);
        sb.Append(":</span><br/>");
        sb.Append("<input name='" + id + "' class='webgis-input webgis-autocomplete' data-source='" + source + "' /><br/>");

        return sb.ToString();
    }

    public static string Text(string text, bool newLine = true)
    {
        return $"<span>{text}</span>{(newLine ? "</br>" : "")}";
    }

    public static string ErrorMessage(string text)
    {
        return $"<div style='color:red;border:2px solid red;border-radius:4px;padding:6px;background:#fee;display:inline-block'><strong>{text}</strong></div></br>";
    }

    public static string WarningMessage(string text)
    {
        return $"<div style='color:#333;border:2px solid #ffa;border-radius:4px;padding:6px;background:#ffe;display:inline-block'><strong>{text}</strong></div></br>";
    }

    public static string LineBreak(int count = 1)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            sb.Append("<br/>");
        }

        return sb.ToString();
    }

    public enum HeaderType
    {
        h1, h2, h3, h4, h5, h6
    }

    public enum UrlSchemaType
    {
        Default,
        ForceHttps,
        Remove
    }

    public static string RequestUrl(IHttpRequestWrapper request, UrlSchemaType schemaType)
    {
        var url = request.Url.ToString();

        if (schemaType == UrlSchemaType.ForceHttps && url.StartsWith("http://"))
        {
            url = "https://" + url.Substring("http://".Length);
        }
        else if (schemaType == UrlSchemaType.Remove)
        {
            if (url.StartsWith("http://"))
            {
                url = url.Substring("http:".Length);
            }
            else if (url.StartsWith("https://"))
            {
                url = url.Substring("https:".Length);
            }
        }

        return url;
    }

    #region Sub Classes

    public class Form : IDisposable
    {
        private StringBuilder _sb;
        private string _method;

        public Form(StringBuilder sb, string action, string method = "post")
        {
            _sb = sb;
            _method = method;
            _sb.Append("<form action='" + action + "' method='" + method + "'>");
        }

        #region IDisposable Member

        public void Dispose()
        {
            _sb.Append("<br/><br/><input type='checkbox' id='__usejson' name='__usejson' /><label for='__usejson' style='display:inline'>&nbsp;Json Response</label><br/>");

            _sb.Append("<span>GeometryType</span><br/>");
            _sb.Append("<select name='__geometryType'>");
            foreach (var geometryType in Enum.GetNames(typeof(QueryGeometryType)))
            {
                _sb.Append("<option value='" + geometryType.ToLower() + "'>" + geometryType + "</option>");
            }
            _sb.Append("</select><br/><br/>");

            _sb.Append("<button type='submit' >" + _method + "</button>");
            _sb.Append("</form>");
        }

        #endregion
    }

    #endregion
}