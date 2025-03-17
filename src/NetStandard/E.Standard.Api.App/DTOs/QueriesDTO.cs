using E.Standard.Api.App.Models.Abstractions;

namespace E.Standard.Api.App.DTOs;

public sealed class QueriesDTO : VersionDTO, IHtml
{
    public QueryDTO[] queries { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        return HtmlHelper.ToList(this.queries, "Queries");
    }

    #endregion
}