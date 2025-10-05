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
                            IApiButtonResources
{
    #region IApiServerButton

    public string Name => "Time Filter";

    public string Container => "Query";

    public string Image => UIImageButton.ToolResourceImage(this, "filter");

    public string ToolTip => "Filter data by time";

    public bool HasUI => true;

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<TimeFilter> localizer)
    {
        return new ApiEventResponse()
            .AddUIElements(
                new UITimeFilterControlElement(),
                new UITimeFilterListElement());
    }

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("filter", Properties.Resources.filter);
    }

    #endregion
}
