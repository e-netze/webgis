using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools.Presentation;

[Export(typeof(IApiButton))]
//[ToolHelp("tools/presentation/timefilter.html")]
[AdvancedToolProperties(VisFilterDependent = true, ClientDeviceDependent = true)]
internal class TimeFilter : IApiServerButtonLocalizable<TimeFilter>,
                            IApiButtonResources,
                            IApiButtonDependency
{
    #region IApiServerButton

    public string Name => "Time Filter";

    public string Container => "Query";

    public string Image => UIImageButton.ToolResourceImage(this, "timefilter");

    public string ToolTip => "Filter data by time";

    public bool HasUI => true;

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<TimeFilter> localizer)
    {
        return new ApiEventResponse()
            .AddUIElements(
                new UICard(localizer.Localize("define-service-filter"))
                    .AddChildren(new UITimeFilterControlElement()),
                new UIBreak(1),
                new UITimeFilterListElement());
    }

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("timefilter", Properties.Resources.timefilter);
    }

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies => VisibilityDependency.HasTimeFilterServices;

    #endregion
}
