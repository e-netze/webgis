using E.Standard.Web.Abstractions;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Data;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableField))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldData))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldDataMulti))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldDateTime))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldExpression))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldHotlink))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldImage))]
public abstract class TableField
{
    public string ColumnName { get; set; }

    public bool Visible { get; set; }

    public abstract string RenderField(WebMapping.Core.Feature feature, NameValueCollection requestHeaders);

    public abstract Task InitRendering(IHttpService httpService);

    public abstract IEnumerable<string> FeatureFieldNames
    {
        get;
    }
}