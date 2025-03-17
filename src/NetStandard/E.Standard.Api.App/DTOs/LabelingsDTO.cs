using E.Standard.Api.App.Models.Abstractions;

namespace E.Standard.Api.App.DTOs;

public sealed class LabelingsDTO : VersionDTO, IHtml
{
    public LabelingDTO[] labelings { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        return HtmlHelper.ToList(this.labelings, "Labelings");
    }

    #endregion
}