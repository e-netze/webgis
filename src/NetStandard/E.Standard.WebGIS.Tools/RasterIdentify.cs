using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Helpers;
using E.Standard.WebGIS.Tools.Identify.Abstractions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(ClientDeviceDependent = true)]
public class RasterIdentify : IApiServerToolLocalizableAsync<RasterIdentify>, IIdentifyTool, IApiButtonResources
{
    #region IApiServerTool Member

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<RasterIdentify> localizer)
    {
        return Task.FromResult(new ApiEventResponse()
            .AddUIElements(
                new UIHidden()
                    .WithId("rasteridentify-counter")
                    .AsToolParameter()
                    .WithValue(1),
                new UIButtonContainer(
                    new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removetoolqueryresults)
                        .WithStyles(UICss.CancelButtonStyle)
                        .WithText(localizer.Localize("remove-markers")))));
    }

    async public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<RasterIdentify> localizer)
    {
        var click = e.ToMapProjectedClickEvent();
        var clickPoint = new Point(click.Longitude, click.Latitude);

        var counter = e.AsDefaultTool ? 0 : int.Parse(e["rasteridentify-counter"]);

        List<IUISetter> uiSetters = new List<IUISetter>();

        var results = await new RasterQueryHelper().PerformHeightQueryAsync(bridge, clickPoint, bridge.AppEtcPath + @"/heightAboveDatum/default.xml");

        var features = new WebMapping.Core.Collections.FeatureCollection();

        var feature = new WebMapping.Core.Feature()
        {
            Shape = clickPoint,
            GlobalOid = Guid.NewGuid().ToString()
        };
        feature.Attributes.Add(new WebMapping.Core.Attribute("_fulltext", $"{localizer.Localize("elevation-query")}:<br/>" + String.Join(@"\n", results.Select(r => r.ResultString)).Replace(@"\n", "<br/>")));
        features.Add(feature);

        return new ApiFeaturesEventResponse(this)
            .AddFeatures(features, e.UseMobileBehavior() ? FeatureResponseType.New : FeatureResponseType.Append)
            .AddClickEvent(click)
            .ZoomToFeaturesResult(false)
            .AddUISetter(new UISetter("rasteridentify-counter", (++counter).ToString()));
    }

    #endregion

    #region IApiTool Member

    public ToolType Type => ToolType.click;

    public ToolCursor Cursor => ToolCursor.Crosshair;

    #endregion

    #region IApiButton Member

    public string Name => "Elevation Point";

    public string Container => "Query";

    public string Image => UIImageButton.ToolResourceImage(this, "heightabovedatum");

    public string ToolTip => "Query elevations from elevation model";

    public bool HasUI => true;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("heightabovedatum", Properties.Resources.heightabovedatum);
    }

    #endregion

    #region IIdenfify 

    public Task<IEnumerable<CanIdentifyResult>> CanIdentifyAsync(IBridge bridge, Point point, double scale, string[] availableServiceIds = null, string[] availableQueryIds = null)
    {
        return Task.FromResult<IEnumerable<CanIdentifyResult>>(new CanIdentifyResult[] {
            new CanIdentifyResult() { Count=1 }
        });
    }

    #endregion
}
