using E.Standard.Api.App.Models.Abstractions;

namespace E.Standard.Api.App.DTOs;

public sealed class PresentationsDTO : VersionDTO, IHtml
{
    public PresentationDTO[] presentations { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        return HtmlHelper.ToList(presentations, "Presentations");
    }

    #endregion
}