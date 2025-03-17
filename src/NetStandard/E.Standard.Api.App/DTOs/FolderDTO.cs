using E.Standard.Api.App.Models.Abstractions;

namespace E.Standard.Api.App.DTOs;

public sealed class FolderDTO : VersionDTO, IHtml
{
    public string id { get; set; }
    public string name { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        return HtmlHelper.ToNextLevelLink(this.id, this.name);
    }

    #endregion
}