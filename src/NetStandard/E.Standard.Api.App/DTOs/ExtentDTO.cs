using E.Standard.Api.App.Models.Abstractions;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class ExtentDTO : VersionDTO, IHtml
{
    public string id { get; set; }
    public double[] extent { get; set; }
    public double[] bounds { get; set; }
    public int? epsg { get; set; }
    public string p4 { get; set; }
    public double[] resolutions { get; set; }
    public double[] origin
    {
        get;
        set;
    }

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToTable(
            new string[] { "Id", "Bounds", "ESPG", "P4 Parameters", "Resolutions", "Origin", "Link" },
            new object[] { this.id, this.extent, this.epsg, this.p4, this.resolutions, this.origin, HtmlHelper.ToNextLevelLink(this.id, this.id) }));

        return sb.ToString();
    }

    #endregion
}