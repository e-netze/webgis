using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Identify;
using E.Standard.WebGIS.Tools.Presentation.Extensions;
using E.Standard.WebGIS.Tools.Presentation.Models;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Comparer;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Presentation;

[Export(typeof(IApiButton))]
[ToolHelp("tools/presentation/visfilter.html")]
[AdvancedToolProperties(VisFilterDependent = true, ClientDeviceDependent = true)]
public class VisFilter : IApiServerButtonLocalizableAsync<VisFilter>,
                         IApiButtonResources
{
    public const string VisFilterQueryBuilderId = "set-toc-layer-filter";
    public const string VisFilterWhereClauseArgumentName = "visfilter-whereclause";

    #region IApiServer Button

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<VisFilter> localizer)
    {
        if (!String.IsNullOrEmpty(e["visfilter-filter"]))
        {
            return await OnVisFilterChanged(bridge, e, localizer);
        }

        return new ApiEventResponse()
            .AddUIElement(
                new UICard(localizer.Localize("predefined-visfilters"))
                    .AddChild(
                        new UIVisFilterCombo() { onchange = "visfilterchanged" }
                            .WithId("visfilter-filter")
                            .AsPersistantToolParameter(UICss.ToolInitializationParameterImportant))
                    );

    }

    public string Container => "Query";

    public bool HasUI => true;

    public string Image => UIImageButton.ToolResourceImage(this, "filter");

    public string Name => "Display Filters";

    public string ToolTip => "Set display filters to restrict the visibility of geo-objects.";

    #endregion

    #region Commands

    [ServerToolCommand("init")]
    [ServerToolCommand("visfilterchanged")]
    async public Task<ApiEventResponse> OnVisFilterChanged(IBridge bridge, ApiToolEventArguments e, ILocalizer<VisFilter> localizer)
    {
        string visFilterParameter = e["visfilter-filter"];

        ApiEventResponse uiResponse = new ApiEventResponse();
        await AppendUI(bridge, uiResponse, localizer, visFilterParameter, bridge.GetVisFilters().Select(f => f.Id).ToArray());

        return uiResponse;
    }

    [ServerToolCommand("autocomplete")]
    async public Task<ApiEventResponse> OnAutocomplete(IBridge bridge, ApiToolEventArguments e)
    {
        string visFilterParameter = e["visfilter_filter"];
        string parameter = e["visfilter_parameter"];

        var serviceId = visFilterParameter.Contains("~") ? visFilterParameter.Split('~')[0] : "";
        var service = await bridge.GetService(serviceId);

        var filter = visFilterParameter.Contains("~") ? bridge.ServiceVisFilter(serviceId, visFilterParameter.Split('~')[1]) : null;

        string lookupLayerName = filter?.LookupLayerName;
        var lookupLayer = String.IsNullOrEmpty(lookupLayerName) ?
                                 null :
                                 service?.Layers?
                                    .Where(l => lookupLayerName.Equals(l.Name, StringComparison.OrdinalIgnoreCase))
                                    .FirstOrDefault();

        Dictionary<string, string> replacments = new Dictionary<string, string>();
        foreach (var keyParameter in filter.Parameters.Keys)
        {
            var val = e[ToValidId(keyParameter)];
            replacments.Add(keyParameter, val ?? String.Empty);
        }

        var values = await bridge.GetLookupValues<string>(filter, e["term"],
                                                          parameter,
                                                          replacments,
                                                          serviceId: serviceId, layerId: lookupLayer?.Id);

        return new ApiRawJsonEventResponse(values.Keys.ToAutocompleteItems().ToArray());
    }

    async private Task AppendUI(IBridge bridge, ApiEventResponse apiResponse, ILocalizer<VisFilter> localizer, string visFilterParameter, IEnumerable<string> activeVisFilters)
    {
        var serviceId = visFilterParameter.Contains("~") ? visFilterParameter.Split('~')[0] : "";
        var service = await bridge.GetService(serviceId);

        var filter = visFilterParameter.Contains("~") ? bridge.ServiceVisFilter(serviceId, visFilterParameter.Split('~')[1]) : null;

        string lookupLayerName = filter?.LookupLayerName;
        var lookupLayer = String.IsNullOrEmpty(lookupLayerName) ?
                                 null :
                                 service?.Layers?
                                    .Where(l => lookupLayerName.Equals(l.Name, StringComparison.OrdinalIgnoreCase))
                                    .FirstOrDefault();

        var existingVisFilter = bridge.GetVisFilters().Where(f => f.Id == visFilterParameter).FirstOrDefault();
        bool visFilterIsActive = activeVisFilters.Contains(visFilterParameter);

        var predefinedFiltersUIParent = new UICard(localizer.Localize("predefined-visfilters"));
        apiResponse.AddUIElements(predefinedFiltersUIParent, new UIBreak(1), new UIVisFilterListElement());

        predefinedFiltersUIParent.AddChild(
            new UIVisFilterCombo() { onchange = "visfilterchanged" }
                .WithId("visfilter-filter")
                .AsPersistantToolParameter()
                .WithValue(visFilterParameter)
        );

        apiResponse.AddUISetter(new UIPersistentParametersSetter(this));

        if (filter != null)
        {
            predefinedFiltersUIParent.AddChild(new UIBreak(1));

            foreach (var keyParemeter in filter.Parameters.Keys)
            {
                predefinedFiltersUIParent.AddChild(
                    new UILabel()
                        .WithLabel(keyParemeter));

                var lookupType = filter.LookupType(keyParemeter);
                var id = ToValidId(keyParemeter);

                #region Current Value Setter

                var currentValue = bridge.CurrentEventArguments[id];

                switch (bridge.CurrentEventArguments["_method"])
                {
                    case "setfilter":
                        //currentValue = bridge.CurrentEventArguments[id];
                        break;
                    case "init":
                    case "visfilterchanged":
                        var argument = existingVisFilter?.Arguments?.Where(a => a.Name == keyParemeter).FirstOrDefault();
                        if (argument != null)
                        {
                            currentValue = argument.Value;
                        }
                        break;
                }

                apiResponse.AddUISetter(new UISetter(id, currentValue));

                #endregion

                if (lookupType == LookupType.None)
                {
                    predefinedFiltersUIParent.AddChild(
                        new UIInputText()
                            .WithId(id)
                            .AsPersistantToolParameter());
                }
                else if (lookupType == LookupType.Autocomplete)
                {
                    predefinedFiltersUIParent.AddChild(
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, typeof(VisFilter), "autocomplete", new
                        {
                            visfilter_filter = visFilterParameter,
                            visfilter_parameter = keyParemeter
                        }))
                            .WithId(id)
                            .AsPersistantToolParameter());
                }
                else if (lookupType == LookupType.ComboBox)
                {
                    var values = await bridge.GetLookupValues<string>(filter, parameter: keyParemeter, serviceId: serviceId, layerId: lookupLayer?.Id);

                    predefinedFiltersUIParent.AddChild(
                        new UISelect()
                            .WithId(id)
                            .AsPersistantToolParameter()
                            .AddOptions(values.Keys.Select(k => new UISelect.Option()
                                                                            .WithValue(k)
                                                                            .WithLabel(values[k]))));
                }
            }
        }


        if (filter != null)
        {
            var buttonContainer = new UIButtonContainer();

            if (visFilterIsActive)
            {
                buttonContainer.AddChild(
                    new UICallbackButton(this, "unsetfilter")
                        .WithText(localizer.Localize("remove-filter"))
                        .WithStyles(UICss.CancelButtonStyle));
            }
            buttonContainer.AddChild(
                new UICallbackButton(this, "setfilter")
                    .WithText(visFilterIsActive
                        ? localizer.Localize("apply-changes")
                        : localizer.Localize("apply-filter")));

            predefinedFiltersUIParent.AddChildren(buttonContainer);
        }
        else
        {
            //var buttonContainer = new UIButtonContainer();

            //foreach (var activeVisFilter in activeVisFilters)
            //{
            //    var activeFilter = activeVisFilter.Contains("~") ? bridge.ServiceVisFilter(activeVisFilter.Split('~')[0], activeVisFilter.Split('~')[1]) : null;
            //    if (activeFilter != null)
            //    {
            //        buttonContainer.AddChild(
            //            new UICallbackButton(this, "unsetfilter") { buttoncommand_argument = activeVisFilter }
            //                .WithText(String.Format(localizer.Localize("filter-remove"), activeFilter.Name))
            //                .WithStyles(UICss.CancelButtonStyle));
            //    }
            //}

            //buttonContainer.AddChild(
            //    new UICallbackButton(this, "unsetfilter") { buttoncommand_argument = "#" }
            //        .WithText(localizer.Localize("remove-all-filter"))
            //        .WithStyles(UICss.CancelButtonStyle));

            //predefinedFiltersUIParent.AddChildren(buttonContainer);
        }
    }

    [ServerToolCommand("setfilter")]
    async public Task<ApiEventResponse> OnSetFilter(IBridge bridge, ApiToolEventArguments e, ILocalizer<VisFilter> localizer)
    {
        string visFilterParameter = e["visfilter-filter"];

        var filter = visFilterParameter.Contains("~") ? bridge.ServiceVisFilter(visFilterParameter.Split('~')[0], visFilterParameter.Split('~')[1]) : null;

        if (filter != null)
        {
            var visFilterDefinition = new VisFilterDefinitionDTO()
            {
                Id = visFilterParameter
            };

            foreach (var keyParameter in filter.Parameters?.Keys.OrEmpty())
            {
                var val = e[ToValidId(keyParameter)];

                if (!String.IsNullOrWhiteSpace(val))
                {
                    visFilterDefinition.AddArgument(keyParameter, val);
                }
            }

            #region Layer Visibility

            var serviceId = visFilterParameter.Contains("~") ? visFilterParameter.Split('~')[0] : "*";
            LayerVisibility layerVisibility = null;

            if (serviceId != "*" && filter.SetLayersVisible)
            {
                var service = await bridge.GetService(serviceId);

                layerVisibility = new LayerVisibility()
                        .AddVisibleLayers(service?.Id, filter.LayerNames?
                            .Select(layerName => service?.Layers?.Where(l => l.Name == layerName).FirstOrDefault())
                            .Where(layer => layer != null)
                            .Select(layer => layer.Id));
            }

            #endregion

            var response = new ApiEventResponse()
                .DoRefreshServices(serviceId)
                .DoRefreshSelection()
                .DoRefreshSnapping()
                .AddLayerVisibility(layerVisibility)
                .AddFilters(visFilterDefinition);

            var activeVisFilters = new List<string>(bridge.GetVisFilters().Select(f => f.Id))
            {
                visFilterParameter
            };

            await AppendUI(bridge, response, localizer, visFilterParameter, activeVisFilters);

            response.RemoveSecondaryToolUI = e.UseSimpleToolsBehaviour();

            return response;
        }

        return null;
    }

    [ServerToolCommand("unsetfilter")]
    async public Task<ApiEventResponse> OnUnsetFilter(IBridge bridge, ApiToolEventArguments e, ILocalizer<VisFilter> localizer)
    {
        string visFilterParameter = e.ServerCommandArgument.OrTake(e["visfilter-filter"]);

        var response = new ApiEventResponse()
            .DoRefreshServices(visFilterParameter.Contains("~") ? visFilterParameter.Split('~')[0] : "*")
            .DoRefreshSelection()
            .DoRefreshSnapping()
            .RemoveFilters(new VisFilterDefinitionDTO()
            {
                Id = visFilterParameter == "#" ? "" : visFilterParameter
            });


        var activeVisFilters = visFilterParameter == "#" ? new List<string>() : new List<string>(bridge.GetVisFilters().Select(f => f.Id));
        activeVisFilters.Remove(visFilterParameter);

        await AppendUI(bridge, response, localizer, e["visfilter-filter"], activeVisFilters);

        response.RemoveSecondaryToolUI = e.UseSimpleToolsBehaviour() && (e["visfilter-filter"] != "#" || e.ServerCommandArgument == "#");  // Wenn einzelen Filter entfernt oder alle entfernt werden => Dialog schließen

        return response;
    }

    [ServerToolCommand("setfeaturefilter")]
    async public Task<ApiEventResponse> OnSetFeatureFilter(IBridge bridge, ApiToolEventArguments e)
    {
        var ids = e["feature_oid"]?.Split(':');
        var filterId = e["filter_id"];

        if (ids.Length == 3)
        {
            var query = await bridge.GetQuery(ids[0], ids[1]);
            if (query == null)
            {
                throw new Exception("Internal error: can't find filter query");
            }

            var oidFilter = new ApiOidFilter(long.Parse(ids[2]));

            using (var context = bridge.UnloadRequestVisFiltersContext())
            {
                //
                // Übergebene Vis Filter für diese Abfrage entfernen, sonst liefert der Id Filter keine Ergebnisse
                //
                var features = await query.PerformAsync(bridge.RequestContext, oidFilter);

                if (features.Count != 1)
                {
                    throw new Exception("Internal error: can't query feature to filter");
                }

                List<FilterDefinitionDTO> visFilterDefinitions = new List<FilterDefinitionDTO>();
                LayerVisibility layerVisibility = null;

                foreach (var serviceFilter in await bridge.ServiceQueryVisFilters(ids[0], ids[1]))
                {
                    if (!String.IsNullOrEmpty(filterId))
                    {
                        if (serviceFilter.Id.ToUniqueFilterId(ids[0]) != filterId)
                        {
                            continue;
                        }
                    }

                    var visFilterDefinition = new VisFilterDefinitionDTO()
                    {
                        Id = serviceFilter.Id.ToUniqueFilterId(ids[0])
                    };

                    foreach (var parameter in serviceFilter.Parameters.Keys)
                    {
                        var val = features[0][parameter];
                        visFilterDefinition.AddArgument(parameter, val);
                    }

                    visFilterDefinitions.Add(visFilterDefinition);

                    #region Layer Visibility

                    if (serviceFilter.SetLayersVisible)
                    {
                        var service = await bridge.GetService(ids[0]);

                        layerVisibility = (layerVisibility ?? new LayerVisibility())
                            .AddVisibleLayers(service?.Id, serviceFilter.LayerNames?
                                .Select(layerName => service?.Layers?.Where(l => l.Name == layerName).FirstOrDefault())
                                .Where(layer => layer != null)
                                .Select(layer => layer.Id));
                    }

                    #endregion
                }

                if (visFilterDefinitions.Count > 0)
                {
                    return new ApiEventResponse()
                        .DoRefreshServices(ids[0])
                        .DoRefreshSelection()
                        .DoRefreshSnapping()
                        .AddLayerVisibility(layerVisibility)
                        .AddFilters(visFilterDefinitions);
                }
            }
        }
        return null;
    }

    [ServerToolCommand("unsetfeaturefilter")]
    async public Task<ApiEventResponse> OnUnsetFeatureFilter(IBridge bridge, ApiToolEventArguments e)
    {
        var ids = e["feature_oid"]?.Split(':');

        if (ids.Length == 3)
        {
            List<FilterDefinitionDTO> visFilterDefinitions = new List<FilterDefinitionDTO>();

            foreach (var serviceFilter in await bridge.ServiceQueryVisFilters(ids[0], ids[1]))
            {
                var visFilterDefinition = new VisFilterDefinitionDTO()
                {
                    Id = serviceFilter.Id.ToUniqueFilterId(ids[0])
                };

                visFilterDefinitions.Add(visFilterDefinition);
            }

            if (visFilterDefinitions.Count > 0)
            {
                return new ApiEventResponse()
                    .DoRefreshServices(ids[0])
                    .DoRefreshSelection()
                    .DoRefreshSnapping()
                    .RemoveFilters(visFilterDefinitions);
            }
        }
        return null;
    }

    [ServerToolCommand("set_toc_layer_filter")]
    async public Task<ApiEventResponse> OnSetTocLayerFilter(IBridge bridge, ApiToolEventArguments e, ILocalizer<VisFilter> localizer)
    {
        var argument = e.ServerCommandArgument;

        var tocVisFilterRequest = JSerializer.Deserialize<TocVisFilterRequestDTO>(argument);
        List<IField> commonFields = null;

        if (tocVisFilterRequest.ServiceLayers == null || tocVisFilterRequest.ServiceLayers.Count == 0)
        {
            return null;
        }

        foreach (var serviceId in tocVisFilterRequest.ServiceLayers.Keys)
        {
            var service = bridge.GetService(serviceId);

            foreach (var layerId in tocVisFilterRequest.ServiceLayers[serviceId])
            {
                var fields = (await bridge.GetServiceLayerFields(serviceId, layerId))?.Where(f => f.Type != WebMapping.Core.FieldType.Shape);
                if (fields == null || fields?.Count() == 0) continue;

                commonFields = commonFields is null
                    ? new List<IField>(fields)
                    : commonFields.Intersect(fields, new FieldNameAndTypeEqualityComparer()).ToList();
            }
        }

        var uiQueryBuilder = new UIQueryBuilder()
        {
            id = VisFilterQueryBuilderId,
            ShowGeometryOption = false,
            CallbackToolId = this.GetType().ToToolId(),
            CallbackArgument = argument
        };

        foreach (var field in commonFields.OrEmpty().OrderBy(f => f.Name))
        {
            uiQueryBuilder.TryAddField(field.Name, field.Type);
        }

        string whereClause = e[VisFilterWhereClauseArgumentName]
                             .IfNullOrEmptyTake(() => tocVisFilterRequest.TryGetWhereClause(bridge.RequestVisFilterDefintions()));
        if(!String.IsNullOrEmpty(whereClause))
        {
            uiQueryBuilder.TrySetWhereClause(whereClause);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
                {
                    new UIDiv()
                    {
                        target =  UIElementTarget.modaldialog.ToString(),
                        targettitle = localizer.Localize("querybuilder"),
                        targetwidth = "600px",
                        targetheight = "400px",
                        elements = new IUIElement[] { uiQueryBuilder }
                    }
                }
        };
    }

    [ServerToolCommand("edit_toc_layer_filter")]
    public Task<ApiEventResponse> OnEditTocLayerFilter(IBridge bridge, ApiToolEventArguments e, ILocalizer<VisFilter> localizer)
    {
        string spanId = e.ServerCommandArgument;
        var spanFilterDefinitions = (bridge.RequestVisFilterDefintions()?
            .Where(f => f.SpanId == spanId)
            .ToArray())
            .ThrowIfNullOrEmpty(() => "No Filterdefinitions found");

        string whereClause = spanFilterDefinitions.First().Arguments?.FirstOrDefault()?.Value;
        var request = new TocVisFilterRequestDTO();
        request.ServiceLayers = new Dictionary<string, string[]>();

        foreach (var serviceId in spanFilterDefinitions.Select(f => f.TocVisFilterServiceId()).Distinct())
        {
            request.ServiceLayers[serviceId] = spanFilterDefinitions
                                                    .Where(f => f.TocVisFilterServiceId() == serviceId)
                                                    .Select(f => f.TocVisFilterLayerId())
                                                    .Where(l => !String.IsNullOrEmpty(l))
                                                    .ToArray();
        }

        e.SetServerCommandArgument(JSerializer.Serialize(request));
        e[VisFilterWhereClauseArgumentName] = whereClause;

        return OnSetTocLayerFilter(bridge, e, localizer);
    }

    [ServerToolCommand("querybuilder")]
    async public Task<ApiEventResponse> OnQueryBuilder(IBridge bridge, ApiToolEventArguments e)
    {
        var argument = e.ServerCommandArgument;
        var tocVisFilterRequest = JSerializer.Deserialize<TocVisFilterRequestDTO>(argument);
        var queryBuilderResult = e.QueryBuilderResult(VisFilterQueryBuilderId);

        if(tocVisFilterRequest.ServiceLayers == null || tocVisFilterRequest.ServiceLayers.Count == 0)
        {
            return null;
        }

        e.AppendIdentifyWhereClauseFromQueryBuilder(queryBuilderResult);
        var whereClause = e[IdentifyDefault.IdentifyWhereClause];
        var spanId = Guid.NewGuid().ToString();

        var visFilterDefinitions = new List<FilterDefinitionDTO>();

        foreach (var serviceId in tocVisFilterRequest.ServiceLayers.Keys)
        {
            foreach (var layerId in tocVisFilterRequest.ServiceLayers[serviceId])
            {
                var fields = await bridge.GetServiceLayerFields(serviceId, layerId);
                if(fields.Count() == 0) continue;

                var visFilterDefinition = new VisFilterDefinitionDTO()
                {
                    Id = VisFilterDefinitionDTO.CreateTocFilterId(serviceId,layerId),
                    ServiceId = serviceId,
                    SpanId = spanId
                };
                visFilterDefinition.AddArgument("sql", whereClause);
                visFilterDefinition.Signature = visFilterDefinition.CalcSignature(bridge.CryptoService);
                visFilterDefinitions.Add(visFilterDefinition);

            }
        }

        if (visFilterDefinitions.Count > 0)
        {
            return new ApiEventResponse()
                .DoRefreshServices(tocVisFilterRequest.ServiceLayers.Keys)
                .DoRefreshSelection()
                .DoRefreshSnapping()
                .AddFilters(visFilterDefinitions)
                .AddUIElement(new UIEmpty().WithTarget(UIElementTarget.modaldialog.ToString()));
        }

        return null;
    }

    #endregion

    #region IApiToolPersistenceContext Member

    public Type PersistenceContextTool => typeof(VisFilter);


    #endregion

    #region Helper

    private string ToValidId(string keyParameter)
    {
        foreach (var c in ".,;ßöüä ".ToArray())
        {
            keyParameter = keyParameter.Replace(c, '-');
        }
        return "visfilter-parameter-" + keyParameter;
    }

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("filter", Properties.Resources.filter);
    }

    #endregion
}
