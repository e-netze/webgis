using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Data;

public sealed class TableFieldDateTime : TableField
{
    public string FieldName { get; set; }

    public override Task InitRendering(IHttpService httpService) => Task.CompletedTask;

    public DateFieldDisplayType DisplayType { get; set; }

    public string FormatString { get; set; }

    public override string RenderField(WebMapping.Core.Feature feature, NameValueCollection requestHeaders)
    {
        try
        {
            var value = feature[this.FieldName];

            if (String.IsNullOrEmpty(value?.Trim()))
            {
                return String.Empty;
            }

            DateTime td = default;
            if (!value.TryParseExactEsriDate(out td))
            {
                td = Convert.ToDateTime(value);
            }
            
            switch (DisplayType)
            {
                case DateFieldDisplayType.ShortDate:
                    return td.ToShortDateString();
                case DateFieldDisplayType.LongDate:
                    return td.ToLongDateString();
                case DateFieldDisplayType.ShortTime:
                    return td.ToShortTimeString();
                case DateFieldDisplayType.LongTime:
                    return td.ToLongTimeString();
            }

            return String.IsNullOrEmpty(this.FormatString) ?
                td.ToString() :
                td.ToString(this.FormatString);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public override IEnumerable<string> FeatureFieldNames
    {
        get
        {
            return new string[] { this.FieldName };
        }
    }
}
