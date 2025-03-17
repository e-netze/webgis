using E.Standard.Web.Abstractions;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace E.Standard.Api.App.DTOs;

[System.Text.Json.Serialization.JsonPolymorphic()]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldData))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldDataMulti))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldDateTime))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldExpressionDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldHotlinkDTO))]
[System.Text.Json.Serialization.JsonDerivedType(typeof(TableFieldImageDTO))]
public abstract class TableFieldDTO
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