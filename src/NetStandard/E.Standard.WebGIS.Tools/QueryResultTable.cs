using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class QueryResultTable : IApiClientButton, IApiButtonDependency
{
    #region IApiButton Member

    public string Name => "Abfrageergebnisse";

    public string Container => "Abfragen";

    public string Image => "results.png";

    public string ToolTip => "Abfrageergebnisse";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.queryresults;

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies => VisibilityDependency.QueryResultsExists;

    #endregion
}
