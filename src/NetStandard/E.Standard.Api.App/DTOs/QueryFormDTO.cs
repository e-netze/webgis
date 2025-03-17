using E.Standard.Api.App.Models.Abstractions;
using E.Standard.WebGIS.Core.Mvc.Wrapper;
using System;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class QueryFormDTO : IHtml
{
    public QueryFormDTO(IHttpRequestWrapper request, QueryDTO query)
    {
        this.Query = query;
        this.Action = HtmlHelper.RequestUrl(request, HtmlHelper.UrlSchemaType.Remove);
    }

    private QueryDTO Query { get; set; }
    private string Action { get; set; }

    #region IHtmlForm Member

    public string ToHtmlString()
    {
        if (this.Query == null || this.Query.items == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append(HtmlHelper.ToHeader(this.Query.name, HtmlHelper.HeaderType.h2));

        using (new HtmlHelper.Form(sb, this.Action))
        {
            foreach (var item in this.Query.items)
            {
                if (item.visible == false)
                {
                    continue;
                }

                if (item.autocomplete)
                {
                    sb.Append(HtmlHelper.Autocomplete(item.name, item.id, this.Action + "?c=autocomplete&_item=" + item.id));
                }
                else
                {
                    sb.Append(HtmlHelper.Input(item.name, item.id));
                }
            }
        }

        return sb.ToString();
    }

    #endregion
}