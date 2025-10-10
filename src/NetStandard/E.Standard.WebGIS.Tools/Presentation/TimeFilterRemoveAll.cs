using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Presentation;

[Export(typeof(IApiButton))]
internal class TimeFilterRemoveAll : IApiClientButton, IApiButtonResources, IApiButtonDependency
{
    #region IApiButton Member

    public string Name => "Remove Time Filters";

    public string Container => "Query";

    public string Image => UIImageButton.ToolResourceImage(this, "timefilter_remove");

    public string ToolTip => "Remove all time filters";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand => ApiClientButtonCommand.timefilterremoveall;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("timefilter_remove", Properties.Resources.timefilter_remove);
    }

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies => VisibilityDependency.HasTimeFilters;

    #endregion
}
