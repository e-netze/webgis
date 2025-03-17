using E.Standard.Api.App.Models.Abstractions;
using System.Text;

namespace E.Standard.Api.App.DTOs;

public sealed class SRefDTO : IHtml
{
    public int id { get; set; }
    public string name { get; set; }
    public string p4 { get; set; }

    public string axis_x { get; set; }
    public string axis_y { get; set; }

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToHeader(this.name, HtmlHelper.HeaderType.h2));

        sb.Append(HtmlHelper.ToTable(
            new string[] { "Id", "Name", "Proj4 Parameters", "AXIS X", "AXIS Y" },
            new object[] { this.id, this.name, this.p4, this.axis_x, this.axis_y }));

        return sb.ToString();
    }

    #endregion
}