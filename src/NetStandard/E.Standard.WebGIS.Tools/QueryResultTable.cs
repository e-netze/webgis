using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class QueryResultTable : IApiClientButton, IApiButtonDependency
{
    #region IApiButton Member

    public string Name => "Query Results";

    public string Container => "Query";

    public string Image => "results.png";

    public string ToolTip => "Opens the table with the current query results.";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.queryresults;

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies => VisibilityDependency.QueryResultsExists;

    #endregion
}
