using E.Standard.CMS.Core;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace E.Standard.Api.App.DTOs;

public sealed class TableFieldExpressionDTO : TableFieldDTO
{
    public string Expression { get; set; }
    public ColumnDataType ColDataType { get; set; }

    public override Task InitRendering(IHttpService httpService) => Task.CompletedTask;

    public override string RenderField(WebMapping.Core.Feature feature, NameValueCollection requestHeaders)
    {
        string val = WebGIS.CMS.Globals.SolveExpression(feature, this.Expression);

        if (val.Contains("$"))
        {
            val = Eval.ParseEvalExpression(val);
        }

        return val;
    }

    public override IEnumerable<string> FeatureFieldNames
    {
        get
        {
            return Helper.GetKeyParameterFields(this.Expression);
        }
    }
}
