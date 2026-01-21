using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools.Presentation;

[Export(typeof(IApiButton))]
public class VisFilterRemoveAll : IApiClientButton, IApiButtonResources, IApiButtonDependency
{
    #region IApiButton Member

    public string Name => "Remove Display Filters";

    public string Container => "Query";

    public string Image => UIImageButton.ToolResourceImage(this, "filter_remove");

    public string ToolTip => "Remove all display filters";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.visfilterremoveall;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("filter_remove", Properties.Resources.filter_remove);
    }

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies => VisibilityDependency.HasFilters;

    #endregion
}
