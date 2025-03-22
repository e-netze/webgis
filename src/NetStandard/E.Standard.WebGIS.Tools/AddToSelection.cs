using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Identify.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true,
                        ScaleDependent = true,
                        UIElementDependency = true)]
[ToolConfigurationSection("identify")]
public class AddToSelection : IApiServerToolAsync, IApiButtonDependency
{
    #region IApiButton Member

    public string Name => "Selection add";

    public string Container => "Query";

    public string Image => "cursor-plus-26-b.png";

    public string ToolTip => "Add item to the current selection";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand
    {
        get { return ApiClientButtonCommand.addtoselection; }
    }

    #endregion

    #region IApiServerTool

    public ToolType Type => ToolType.click;

    public ToolCursor Cursor => ToolCursor.Pointer;

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        return Task.FromResult<ApiEventResponse>(null);
    }

    async public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        if (!e.MapScale.HasValue)
        {
            throw new ArgumentException("Unknown map scale");
        }

        if (e.SelectionInfo == null)
        {
            throw new ArgumentException("No Features selected");
        }

        var selectedFeatures = await e.FeaturesFromSelectionAsync(bridge);
        if (selectedFeatures.Count < 1)
        {
            throw new ArgumentException("No Features selected");
        }

        var query = await bridge.GetQuery(e.SelectionInfo.ServiceId, e.SelectionInfo.QueryId);
        if (query == null)
        {
            throw new ArgumentException("Unknown query");
        }

        var click = e.ToMapProjectedClickEvent();

        ApiSpatialFilter filter = new ApiSpatialFilter()
        {
            FilterSpatialReference = click.SRef,
            FeatureSpatialReference = bridge.GetSupportedSpatialReference(query, bridge.DefaultSrefId)
        };

        filter.SetClickQueryShape(query, e.MapScale.Value, e);

        var queryFeatures = await query.PerformAsync(bridge.RequestContext, filter);

        var newFeatures = new WebMapping.Core.Collections.FeatureCollection();
        foreach (var queryFeature in queryFeatures)
        {
            if (selectedFeatures.NotContainsFeatureWithOid(queryFeature.Oid))
            {
                newFeatures.Add(queryFeature);
                selectedFeatures.Add(queryFeature);
            }
        }

        if (newFeatures.Count() == 0)
        {
            return null;
        }

        return new ApiFeaturesEventResponse()
            .AddFeatures(newFeatures, FeatureResponseType.Append, appendHoverShapes: e.UseDesktopBehavior())
            .AddFeaturesForLinks(selectedFeatures)
            .AddFeaturesSpatialReference(filter.FeatureSpatialReference)
            .AddFeaturesFilter(filter)
            .AddFeaturesQuery(query)
            .AddClickEvent(click)
            .ZoomToFeaturesResult(false)
            .SelectFeaturesResult();
    }

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies
    {
        get { return VisibilityDependency.HasSelection; }
    }

    #endregion
}
