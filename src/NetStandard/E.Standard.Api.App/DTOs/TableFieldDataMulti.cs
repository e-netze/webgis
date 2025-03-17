using E.Standard.Web.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App.DTOs;

public sealed class TableFieldDataMulti : TableFieldDTO
{
    public string[] FieldNames { get; set; }

    public override Task InitRendering(IHttpService httpService) => Task.CompletedTask;

    public override string RenderField(WebMapping.Core.Feature feature, NameValueCollection requestHeaders)
    {
        StringBuilder sb = new StringBuilder();
        if (FieldNames != null)
        {
            foreach (string fieldName in FieldNames)
            {
                string val = feature[fieldName];
                if (String.IsNullOrEmpty(val))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(val);
            }
        }
        return sb.ToString();
    }

    public override IEnumerable<string> FeatureFieldNames
    {
        get
        {
            return FieldNames;
        }
    }
}
