using E.Standard.Api.App.Models.Abstractions;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class ExtentsDTO : VersionDTO, IHtml
{
    public ExtentDTO[] extents { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToList(extents, "Extents"));

        return sb.ToString();
    }

    #endregion
}