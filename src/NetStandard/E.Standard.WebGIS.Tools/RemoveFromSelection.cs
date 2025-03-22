using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(SelectionInfoDependent = true,
                        ScaleDependent = true)]
[ToolConfigurationSection("identify")]
public class RemoveFromSelection : IApiServerToolAsync, IApiButtonDependency
{
    #region IApiButton Member

    public string Name => "Selection remove";

    public string Container => "Query";

    public string Image => "cursor-minus-26-b.png";

    public string ToolTip => "Remove item from current selection";

    public bool HasUI => false;

    #endregion

    #region IApiClientButton Member

    public ApiClientButtonCommand ClientCommand
    {
        get { return ApiClientButtonCommand.removefromselection; }
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

        List<string> featureOids = new List<string>();
        foreach (var queryFeature in queryFeatures)
        {
            if (selectedFeatures.ContainsFeatureWithOid(queryFeature.Oid))
            {
                featureOids.Add($"{e.SelectionInfo.ServiceId}:{e.SelectionInfo.QueryId}:{queryFeature.Oid}");
            }
        }

        // Empty Features Response: Damit 1:n Links neu gesetzt werden
        // Features = null,
        // FeaturesForLinks = selectedFeatures,
        // Query = query,

        // Eigentlicher Response zum entfernen der Features
        return new ApiFeaturesEventResponse()
            .AddClientCommands(ApiClientButtonCommand.removefromselection)
            .AddClientCommandData(featureOids.ToArray());
    }

    #endregion

    #region Commands

    [ServerToolCommand("refresh-query-links")]
    async public Task<ApiEventResponse> OnRefreshQueryLinks(IBridge bridge, ApiToolEventArguments e)
    {
        var serviceId = e["service"];
        var queryId = e["query"];
        var oids = e["oids"]?.Split(',');

        var query = await bridge.GetQuery(serviceId, queryId);
        if (query != null)
        {
            ApiOidsFilter filter = new ApiOidsFilter(oids.Select(o => int.Parse(o)).ToArray());

            var queryFeatures = await query.PerformAsync(bridge.RequestContext, filter);

            return new ApiFeaturesEventResponse() // Empty Features Response: Damit 1:n Links neu gesetzt werden
                .AddFeaturesForLinks(queryFeatures)
                .AddFeaturesQuery(query);
        }

        return null;
    }

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies
    {
        get { return VisibilityDependency.HasMoreThanOneSelected; }
    }

    #endregion
}
