using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(MapCrsDependent = true)]
[ToolHelp("tools/general/measure-area.html")]
public class MeasureArea : IApiServerTool, IApiButtonResources
{
    #region IApiServerTool Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse();

        if (e.CalcCrs == Epsg.WebMercator)
        {
            response
                .AddUIElements(
                    new UIDiv()
                    {
                        css = UICss.ToClass(new string[] { "webgis-info" }),
                        elements = new IUIElement[]
                            {
                                new UILiteral() { literal="Achtung: Das Koordinatensystem für die Berechnung der Messwerte ist WebMercator. Aufgrund der Längenverzerrungen in dieser Kartenprojektion weichen die Werte stark von der Realität ab!" }
                            }
                    }
                );
        }

        response.AddUIElements(
                new UILabel()
                    .WithLabel("Umfang (m)"),
                new UIInputText()
                    .WithStyles("webgis-sketch-circumference"),
                new UILabel()
                    .WithLabel("Fläche (m²)"),
                new UIInputText()
                    .WithStyles("webgis-sketch-area"),
                new UIButtonContainer(
                    new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removesketch)
                        .WithStyles(UICss.CancelButtonStyle)
                        .WithText("Sketch entfernen")),
                new UISketchInfoContainer());

        return response;
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return null;
    }

    #endregion

    #region IApiTool Member

    public ToolType Type => ToolType.sketch2d;

    public ToolCursor Cursor => ToolCursor.Custom_Pen;

    #endregion

    #region IApiButton Member

    public string Name => "Measure Area";

    public string Container => "Tools";

    public string Image => UIImageButton.ToolResourceImage(this, "measure_area");

    public string ToolTip => "Draw a polygon to measure the area";

    public bool HasUI => true;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("measure_area", Properties.Resources.measure_area);
    }

    #endregion
}
