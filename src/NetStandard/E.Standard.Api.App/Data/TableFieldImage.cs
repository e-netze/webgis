using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;

using E.Standard.CMS.Core;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;

namespace E.Standard.Api.App.Data;

public sealed class TableFieldImage : TableField
{
    public string ImageExpression { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    public override string RenderField(
        WebMapping.Core.Feature feature,
        TableFieldRenderingContext context)
    {
        var imageContext = context as ImageRenderingContext
            ?? throw new ArgumentException(
                $"Invalid rendering context for {nameof(TableFieldImage)}.",
                nameof(context));
        string imgExpression = WebGIS.CMS.Globals.SolveExpression(
            feature,
            imageContext.Expression,
            imageContext.Parameters);
        if (String.IsNullOrWhiteSpace(imgExpression))  // Don't show empty images
        {
            return String.Empty;
        }

        return String.Concat(imageContext.ImageTagPrefix, imgExpression, "' />");
    }

    public override TableFieldRenderingContext CreateRenderingContext(NameValueCollection requestHeaders)
    {
        var expression = ImageExpression.ReplaceUrlHeaderPlaceholders(requestHeaders);
        return new ImageRenderingContext(
            requestHeaders,
            expression,
            Helper.GetKeyParameters(expression),
            CreateImageTagPrefix());
    }

    private string CreateImageTagPrefix()
    {
        var sb = new StringBuilder("<img style='");
        if (this.ImageWidth > 0)
        {
            sb.Append("width:").Append(this.ImageWidth).Append("px;");
        }

        if (this.ImageHeight > 0)
        {
            sb.Append(";height:").Append(this.ImageHeight).Append("px;");
        }

        return sb.Append("' src='").ToString();
    }

    public override IEnumerable<string> FeatureFieldNames
    {
        get
        {
            return Helper.GetKeyParameterFields(this.ImageExpression);
        }
    }

    private sealed class ImageRenderingContext(
        NameValueCollection requestHeaders,
        string expression,
        IReadOnlyList<string> parameters,
        string imageTagPrefix)
        : TableFieldRenderingContext(requestHeaders)
    {
        public string Expression { get; } = expression;
        public IReadOnlyList<string> Parameters { get; } = parameters;
        public string ImageTagPrefix { get; } = imageTagPrefix;
    }
}
