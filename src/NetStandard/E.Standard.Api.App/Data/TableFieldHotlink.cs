using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using E.Standard.CMS.Core;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;

namespace E.Standard.Api.App.Data;

public sealed class TableFieldHotlink : TableField
{
    public string HotlinkUrl { get; set; }
    public string HotlinkName { get; set; }
    public bool One2N { get; set; }
    public char One2NSeperator { get; set; }
    public BrowserWindowTarget Target { get; set; }
    public string ImageExpression { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }

    public override string RenderField(
        WebMapping.Core.Feature feature,
        TableFieldRenderingContext context)
    {
        var hotlinkContext = GetContext(context);
        string url = SolveHotlinkUrl(feature, hotlinkContext);

        if (String.IsNullOrWhiteSpace(url))  // Don't show empty links
        {
            return String.Empty;
        }

        string imgExpression = WebGIS.CMS.Globals.SolveExpression(
            feature,
            hotlinkContext.ImageExpression,
            hotlinkContext.ImageParameters);
        string name = WebGIS.CMS.Globals.SolveExpression(
            feature,
            hotlinkContext.NameExpression,
            hotlinkContext.NameParameters);

        if (String.IsNullOrEmpty(imgExpression))
        {
            return String.Concat(hotlinkContext.AnchorPrefix, url, "'>", name, "</a>");
        }

        return String.Concat(
            hotlinkContext.AnchorPrefix, url, "'>",
            hotlinkContext.ImageTagPrefix, imgExpression, "' />",
            name, "</a>");
    }

    public string SolveHotlinkUrl(
        WebMapping.Core.Feature feature,
        TableFieldRenderingContext context)
    {
        var hotlinkContext = GetContext(context);
        return WebGIS.CMS.Globals.SolveExpression(
            feature,
            hotlinkContext.UrlExpression,
            hotlinkContext.UrlParameters);
    }

    public bool PreparedImageExpressionHasParameters(TableFieldRenderingContext context)
        => GetContext(context).ImageExpressionHasParameters;

    public string PreparedImageExpression(TableFieldRenderingContext context)
        => GetContext(context).ImageExpression;

    public override TableFieldRenderingContext CreateRenderingContext(NameValueCollection requestHeaders)
    {
        var urlExpression = HotlinkUrl.ReplaceUrlHeaderPlaceholders(requestHeaders);
        var imageExpression = ImageExpression.ReplaceUrlHeaderPlaceholders(requestHeaders);
        var nameExpression = String.IsNullOrEmpty(HotlinkName) ? ColumnName : HotlinkName;

        return new HotlinkRenderingContext(
            requestHeaders,
            urlExpression,
            Helper.GetKeyParameters(urlExpression),
            imageExpression,
            Helper.GetKeyParameters(imageExpression),
            ImageExpression?.Contains("[") == true
                && ImageExpression.Contains("]"),
            nameExpression,
            Helper.GetKeyParameters(nameExpression),
            $"<a target='{Target}' href='",
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

        return sb.Append("margin-right:8px;vertical-align:sub' src='").ToString();
    }

    private static HotlinkRenderingContext GetContext(TableFieldRenderingContext context)
        => context as HotlinkRenderingContext
            ?? throw new ArgumentException(
                $"Invalid rendering context for {nameof(TableFieldHotlink)}.",
                nameof(context));

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

    private sealed class HotlinkRenderingContext(
        NameValueCollection requestHeaders,
        string urlExpression,
        IReadOnlyList<string> urlParameters,
        string imageExpression,
        IReadOnlyList<string> imageParameters,
        bool imageExpressionHasParameters,
        string nameExpression,
        IReadOnlyList<string> nameParameters,
        string anchorPrefix,
        string imageTagPrefix)
        : TableFieldRenderingContext(requestHeaders)
    {
        public string UrlExpression { get; } = urlExpression;
        public IReadOnlyList<string> UrlParameters { get; } = urlParameters;
        public string ImageExpression { get; } = imageExpression;
        public IReadOnlyList<string> ImageParameters { get; } = imageParameters;
        public bool ImageExpressionHasParameters { get; } = imageExpressionHasParameters;
        public string NameExpression { get; } = nameExpression;
        public IReadOnlyList<string> NameParameters { get; } = nameParameters;
        public string AnchorPrefix { get; } = anchorPrefix;
        public string ImageTagPrefix { get; } = imageTagPrefix;
    }
}
