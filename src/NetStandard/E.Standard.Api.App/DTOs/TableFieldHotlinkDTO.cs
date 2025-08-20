using E.Standard.CMS.Core;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App.DTOs;

public sealed class TableFieldHotlinkDTO : TableFieldDTO
{
    public string HotlinkUrl { get; set; }
    public string HotlinkName { get; set; }
    public bool One2N { get; set; }
    public char One2NSeperator { get; set; }
    public BrowserWindowTarget Target { get; set; }
    public string ImageExpression { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    public override Task InitRendering(IHttpService httpService) => Task.CompletedTask;

    public override string RenderField(WebMapping.Core.Feature feature, NameValueCollection requestHeaders)
    {
        string url = WebGIS.CMS.Globals.SolveExpression(feature, this.HotlinkUrl.ReplaceUrlHeaderPlaceholders(requestHeaders));

        if (String.IsNullOrWhiteSpace(url))  // Don't show empty links
        {
            return String.Empty;
        }

        string imgExpression = WebGIS.CMS.Globals.SolveExpression(feature, this.ImageExpression.ReplaceUrlHeaderPlaceholders(requestHeaders));
        string imageTag = String.Empty;

        if (!string.IsNullOrEmpty(imgExpression))
        {
            var style = new StringBuilder();

            if (this.ImageWidth > 0)
            {
                style.Append($"width:{this.ImageWidth}px;");
            }

            if (this.ImageHeight > 0)
            {
                style.Append($";height:{this.ImageHeight}px;");
            }

            style.Append("margin-right:8px;vertical-align:sub");

            imageTag = $"<img style='{style}' src='{imgExpression}' />";
        }

        StringBuilder sb = new StringBuilder();

        sb.Append("<a target='");
        sb.Append(Target.ToString());
        sb.Append("' href='");
        sb.Append(url);
        sb.Append("'>");
        sb.Append(imageTag);
        sb.Append(WebGIS.CMS.Globals.SolveExpression(feature, String.IsNullOrEmpty(this.HotlinkName) ? this.ColumnName : this.HotlinkName));
        sb.Append("</a>");

        return sb.ToString();
    }

    public override IEnumerable<string> FeatureFieldNames
    {
        get
        {
            List<string> fields = new List<string>();

            var urlFields = Helper.GetKeyParameterFields(this.HotlinkUrl);
            var nameFields = Helper.GetKeyParameterFields(this.HotlinkName);

            if (urlFields != null && urlFields.Length > 0)
            {
                fields.AddRange(urlFields);
            }

            if (nameFields != null && nameFields.Length > 0)
            {
                fields.AddRange(nameFields);
            }

            return fields.Distinct();
        }
    }
}
