using E.Standard.Api.App.Models.Abstractions;

namespace E.Standard.Api.App.DTOs;

public sealed class EditThemesDTO : VersionDTO, IHtml
{
    public EditThemeDTO[] editthemes { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        return HtmlHelper.ToList(this.editthemes, "EditThemes");
    }

    #endregion
}