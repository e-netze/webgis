using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

using E.Standard.Web.Abstractions;

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

    /// <summary>
    /// Creates the request-local rendering state without I/O. Never store request state on the
    /// field itself: TableField instances are shared through cached queries.
    /// </summary>
    public virtual TableFieldRenderingContext CreateRenderingContext(NameValueCollection requestHeaders)
        => new TableFieldRenderingContext(requestHeaders);

    public virtual Task<TableFieldRenderingContext> CreateRenderingContextAsync(
        IHttpService httpService,
        NameValueCollection requestHeaders)
        => Task.FromResult(CreateRenderingContext(requestHeaders));

    public abstract string RenderField(
        WebMapping.Core.Feature feature,
        TableFieldRenderingContext context);

    public abstract IEnumerable<string> FeatureFieldNames
    {
        get;
    }
}
