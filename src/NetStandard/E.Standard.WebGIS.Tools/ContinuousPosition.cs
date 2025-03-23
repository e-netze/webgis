using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
public class ContinuousPosition : IApiClientToolLocalizable<ContinuousPosition>, 
                                  IApiButtonResources
{
    public string Container => "Navigation";

    public bool HasUI => true;

    public string Image => UIImageButton.ToolResourceImage(this, "cont_pos");

    public string Name => "Track Position";

    public string ToolTip => "Continuous display of the current (GPS) position";

    public ToolType Type => ToolType.watch_position;

    public ToolCursor Cursor => ToolCursor.Pointer;

    #region IApiClientTool

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<ContinuousPosition> localizer)
    {
        return new ApiEventResponse()
            .AddUIElements(
                new UILabel()
                    .WithLabel(localizer.Localize("velocity")),
                new UIInputText()
                    .WithStyles("webgis-continous-position-speed"));
    }

    #endregion

    #region IApiToolResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("cont_pos", E.Standard.WebGIS.Tools.Properties.Resources.cont_pos);
    }

    #endregion
}
