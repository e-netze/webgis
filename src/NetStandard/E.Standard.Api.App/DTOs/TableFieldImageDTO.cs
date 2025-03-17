using E.Standard.CMS.Core;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace E.Standard.Api.App.DTOs;

public sealed class TableFieldImageDTO : TableFieldDTO
{
    public string ImageExpression { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    public override Task InitRendering(IHttpService httpService) => Task.CompletedTask;

    public override string RenderField(WebMapping.Core.Feature feature, NameValueCollection requestHeaders)
    {
        string imgExpression = WebGIS.CMS.Globals.SolveExpression(feature, this.ImageExpression.ReplaceUrlHeaderPlaceholders(requestHeaders));
        if (String.IsNullOrWhiteSpace(imgExpression))  // Don't show empty images
        {
            return String.Empty;
        }

        string style = String.Empty;
        if (this.ImageWidth > 0)
        {
            style += "width:" + this.ImageWidth + "px;";
        }

        if (this.ImageHeight > 0)
        {
            style += ";height:" + this.ImageHeight + "px;";
        }

        return "<img style='" + style + "' src='" + imgExpression + "' />";
    }

    public override IEnumerable<string> FeatureFieldNames
    {
        get
        {
            return Helper.GetKeyParameterFields(this.ImageExpression);
        }
    }
}
