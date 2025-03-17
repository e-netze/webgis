using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[ToolCmsConfigParameter(MeasureCircle.CmsRadiiParameter)]
[ToolHelp("tools/general/circle.html")]
[ToolConfigurationSection("measure-circle")]
public class MeasureCircle : IApiServerTool, IApiButtonResources
{
    const string CmsRadiiParameter = "markercircleradii";

    #region IApiTool

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e) => null;

    public ToolType Type => ToolType.circlemarker;

    public ToolCursor Cursor => ToolCursor.Custom_Pen;

    #endregion

    #region ApiButton

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        List<int> radiiList = new List<int>(e.GetConfigArray<int>("radii") ?? Array.Empty<int>());

        foreach (var radii in bridge.ToolConfigValues<string>(MeasureCircle.CmsRadiiParameter))
        {
            if (!String.IsNullOrEmpty(radii))
            {
                radiiList.AddRange(radii.Split(',').Select(r => int.Parse(r)).Where(r => r > 0).ToArray());
            }
        }

        return new ApiEventResponse()
            .AddUIElements(
                new UILabel().WithLabel("Radius (m)"),
                new UIMarkerCircleRadiusCombo()
                {
                    radii = radiiList.Count > 0 ? radiiList.Distinct().OrderBy(r => r).ToArray() : null
                },
                new UIButtonContainer(
                    new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removecirclemarker)
                        .WithStyles(UICss.CancelButtonStyle)
                        .WithText("Umgebungskreis entfernen")));
    }

    public string Container => "Werkzeuge";

    public bool HasUI => true;

    public string Image => UIImageButton.ToolResourceImage(this, "measure_circle");

    public string Name => "Umgebungskreis";

    public string ToolTip => "Umgebungskreis zeichnen";

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("measure_circle", Properties.Resources.measure_circle);
    }

    #endregion
}
