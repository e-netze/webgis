using E.Standard.Api.App.Models.Abstractions;
using System;
using System.Linq;
using System.Text;

namespace E.Standard.Api.App.DTOs.Tools;

public sealed class ToolCollectionDTO : IHtml
{
    public ToolDTO[] tools { get; set; }

    public string ToHtmlString()
    {
        var sb = new StringBuilder();

        var containers = this.tools.Where(t => !String.IsNullOrEmpty(t.name) && !String.IsNullOrEmpty(t.container) && String.IsNullOrEmpty(t.parentid))
                                   .Select(t => t.container)
                                   .Distinct();

        foreach (var container in containers.OrderBy(c => c))
        {
            sb.Append(HtmlHelper.ToList(
                this.tools.Where(t => t.container == container && !String.IsNullOrEmpty(t.name) && String.IsNullOrEmpty(t.parentid))
                          .OrderBy(t => t.name)
                          .ToArray(),
                container));
        }
        return sb.ToString();
    }
}