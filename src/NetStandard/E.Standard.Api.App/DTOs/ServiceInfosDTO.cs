using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Api.App.Services.Cache;
using System;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class ServiceInfosDTO : VersionDTO, IHtml3
{
    public ServiceInfoDTO[] services { get; set; }
    public string[] unknownservices { get; set; }
    public string[] unauthorizedservices { get; set; }

    public string[] exceptions { get; set; }

    public CopyrightInfoDTO[] copyright { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        return ToHtmlString(null);
    }

    public string ToHtmlString(CacheService cache)
    {
        var html = new StringBuilder();

        html.Append(HtmlHelper.ToList(services));

        if (exceptions != null && exceptions.Length > 0)
        {
            html.Append(HtmlHelper.ToHeader("Service Exceptions:", HtmlHelper.HeaderType.h4));

            html.Append("<ul style='list-style:none;padding:0px>'");
            foreach (var exception in exceptions)
            {
                html.Append("<li>");

                foreach (var p in exception.Split('\n'))
                {
                    html.Append($"<p>{p}</p>");
                }

                html.Append("<li/>");
            }
            html.Append("<ul>");
        }
        else if (cache != null)
        {
            foreach (var serviceInfo in this.services)
            {
                var service = cache.GetOriginalService(serviceInfo.id, null, null).Result;
                if (service != null)
                {
                    html.Append(HtmlHelper.ToHeader("Service Diagnostics:", HtmlHelper.HeaderType.h4));
                    var message = service.DiagnosticMessage() ?? String.Empty;
                    foreach (var p in message.Split('\n'))
                    {
                        html.Append($"<p>{p}</p>");
                    }
                }
            }
        }

        return html.ToString();
    }

    #endregion
}