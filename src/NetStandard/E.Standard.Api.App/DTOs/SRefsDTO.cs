using E.Standard.Api.App.Models.Abstractions;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class SRefsDTO : IHtml
{
    public int[] ids { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        if (ids != null)
        {
            sb.Append("<ul styl>");
            foreach (int id in ids)
            {
                sb.Append("<li style='display:inline-table'>");
                sb.Append(HtmlHelper.ToNextLevelLink(id.ToString(), id.ToString()));
                sb.Append("</li>");
            }
            sb.Append("</ul>");
        }

        return sb.ToString();
    }

    #endregion
}