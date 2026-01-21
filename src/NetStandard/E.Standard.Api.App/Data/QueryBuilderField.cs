using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.Api.App.Data;

public class QueryBuilderField : IQueryBuilderFieldBridge
{
    public string Name { get; set; }
    public string Aliasname { get; set; }
}