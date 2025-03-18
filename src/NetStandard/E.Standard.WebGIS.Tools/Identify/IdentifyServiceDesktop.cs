using E.Standard.Extensions.Compare;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Identify.Abstractions;
using E.Standard.WebGIS.Tools.Identify.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Exceptions;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Identify;

internal class IdentifyServiceDesktop
{
    async public Task<ApiEventResponse> OnEvent(IApiTool tool, IBridge bridge, ApiToolEventArguments e)
    {
        var click = e.ToMapProjectedClickEvent();

        double eventScale = click.EventScale.OrTake(e.GetDouble(IdentifyDefault.IdentifyMapScaleId)).OrTake(e.MapScale ?? 0);
        string allQueries = e[IdentifyDefault.IdentifyAllQueriesId];
        string targetCategoryId = String.Empty;
        bool appendToUI = false;

        double[] mapBBox = e.GetDoubleArray(IdentifyDefault.IdentifyMapBBoxId).OrTake<double>(e.MapBBox());
        int[] mapSize = e.GetArray<int>(IdentifyDefault.IdentifyMapSizeId).OrTake<int>(e.MapSize());
        string[] identifyOptions = e[IdentifyDefault.IdentifyToolOptionsId].TryParseJson<string[]>(new string[0]);
        bool addCheckboxes = e.GetConfigBool(IdentifyDefault.ShowLayerVisibilityCheckboxesConfigKey) || e.GetBoolean(IdentifyDefault.IdentifyForceCheckboxes);
        string filterWhereClause = e[IdentifyDefault.IdentifyWhereClause];

        ApiEventResponse response = null;

        IEnumerable<IQueryBridge> queries = null;
        bool isMultiQuery = true, zoom2results = false, hasExcludedQueries = false;

        #region Get QueryTheme Name

        string queryTheme = e.MenuItemValue;

        if (String.IsNullOrEmpty(queryTheme))
        {
            queryTheme = e[IdentifyDefault.IdentifyQueryThemeId] ?? String.Empty;

            //
            // Always use "#" (visible layers) with default tool (if MenuItemValue is empty)
            // execpt it starts with "!" => comes from favorite exclusion
            //                           => otherweise method can be called again in an endless loop!!
            //
            if (queryTheme.IsIgnoreFavoritesQueryThemeType() == false &&
                e.AsDefaultTool == true)
            {
                queryTheme = IdentifyConst.QueryVisibleDefault;
            }
        }

        if (queryTheme.HasAppendToUIFlag())
        {
            queryTheme = queryTheme.RemoveAppendToUIFlag();
            appendToUI = true;
        }

        #endregion

        #region Collect Queries

        var favItems = await bridge.GetUserFavoriteItemsAsync(tool, "OnEvent");

        if (queryTheme == IdentifyConst.QueryVisibleDefault)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Visible))
                .TakeFavorites(favItems, out hasExcludedQueries);
            targetCategoryId = hasExcludedQueries ? e.FavoritesCategoryId() : e.VisibleCategoryId();
        }
        else if (queryTheme == IdentifyConst.QueryVisibleRemoveFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Visible))
                .RemoveFavorites(favItems, out hasExcludedQueries);
            targetCategoryId = e.VisibleCategoryId();
        }
        else if (queryTheme == IdentifyConst.QueryVisibleIgnoreFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Visible));
            targetCategoryId = e.VisibleCategoryId();
        }
        else if (queryTheme == IdentifyConst.QueryInvisibleDefault)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Invisible))
                .TakeFavorites(favItems, out hasExcludedQueries);
            targetCategoryId = e.InVisbileCategoryId();
        }
        else if (queryTheme == IdentifyConst.QueryInvisibleRemoveFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Invisible))
                .RemoveFavorites(favItems, out hasExcludedQueries);
            targetCategoryId = e.InVisbileCategoryId();
        }
        else if (queryTheme == IdentifyConst.QueryInvisibleIgnoreFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Invisible));
            targetCategoryId = e.InVisbileCategoryId();
        }
        else if (queryTheme == IdentifyConst.QueryAllDefault)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Any))
                .TakeFavorites(favItems, out hasExcludedQueries);
            targetCategoryId = hasExcludedQueries ? e.FavoritesCategoryId() : e.AllCategoryId();
        }
        else if (queryTheme == IdentifyConst.QueryAllRemoveFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Any))
                .RemoveFavorites(favItems, out hasExcludedQueries);
            targetCategoryId = e.AllCategoryId();
        }
        else if (queryTheme == IdentifyConst.QueryAllIgnoreFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Any));
            targetCategoryId = e.AllCategoryId();
        }
        else if (queryTheme.IsIdentifyToolQuery())
        {
            string toolId = queryTheme.ToolIdFromQueryTheme(e);

            e.ClearMenuItemValue();

            IApiTool button = (IApiTool)bridge.TryGetFriendApiButton(tool, toolId);
            if (button is IApiServerTool && !button.GetType().Equals(tool.GetType())) // dont call yourself
            {
                return ((IApiServerTool)button).OnEvent(bridge, e);
            }
            else if (button is IApiServerToolAsync && !button.GetType().Equals(tool.GetType())) // dont call yourself
            {
                return await ((IApiServerToolAsync)button).OnEvent(bridge, e);
            }
            else
            {
                return null;
            }
        }
        else
        {
            var query = await bridge.GetQueryFromThemeId(queryTheme);
            if (query != null)
            {
                queries = new IQueryBridge[] { query };
            }

            isMultiQuery = false;

            await bridge.SetUserFavoritesItemAsync(tool, "OnEvent", queryTheme);
        }

        #endregion

        if (queries == null || queries.Count() == 0)
        {
            if (appendToUI)
            {
                // run everything with empty collection of queries => will result in "Keine Abfrage gerunden" in the UI  
                // and not as modal dialog
                // Otherwise: UI 'loading icon' waits forever
                queries = Array.Empty<IQueryBridge>();
            }
            else
            {
                throw new InfoException("Keine Abfragen gefunden!?");
            }
        }

        #region Add Map SRef, BBox & Size (wichtig für WMS Identify) 

        //var sRef4326 = bridge.CreateSpatialReference(4326);
        foreach (var query in queries)
        {
            if (mapBBox != null && mapBBox.Length == 4 && mapSize != null && mapSize.Length == 2)
            {
                query.SetMapProperties(click.SRef, new Envelope(mapBBox[0], mapBBox[1], mapBBox[2], mapBBox[3]), mapSize[0], mapSize[1]);
            }
        }

        #endregion

        #region Create Filter

        Shape queryShape = click.Sketch;
        if (queryShape != null && queryShape.ShapeEnvelope.HasValidExtent == false)
        {
            queryShape = null;
        }

        queryShape = e.ApplyBuffer(queryShape, click.SRef, e.CalcCrs);

        ApiSpatialFilter filter = new ApiSpatialFilter()
        {
            QueryShape = queryShape,
            FilterSpatialReference = click.SRef,
            //FeatureSpatialReference = click.SRef
        };

        #endregion

        #region Service Ids

        var serviceIds = queries.Where(q => !String.IsNullOrEmpty(q.GetServiceId())).Select(q => q.GetServiceId()).Distinct();

        #endregion

        var found = new Dictionary<IQueryBridge, int>();
        var queryFilters = new Dictionary<IQueryBridge, ApiSpatialFilter>();
        WebMapping.Core.Collections.FeatureCollection features = null;

        var tasks = new Dictionary<IQueryBridge, Task<int>>();

        StringBuilder errors = new StringBuilder();

        #region Perform Queries

        try
        {
            foreach (var serviceId in serviceIds)
            {
                var serviceQueries = queries.Where(q => serviceId.Equals(q.GetServiceId()));

                foreach (var serviceQuery in serviceQueries)
                {
                    var queryFilter = (ApiSpatialFilter)filter.Clone();
                    queryFilter.FeatureSpatialReference = bridge.GetSupportedSpatialReference(serviceQuery, bridge.DefaultSrefId);
                    queryFilters.Add(serviceQuery, queryFilter);

                    if (queryShape == null)
                    {
                        queryFilter.SetClickQueryShape(serviceQuery, eventScale, e);
                    }
                    else
                    {
                        queryFilter.QueryShape = queryShape;
                    }

                    if (e.GetBoolean(IdentifyDefault.IdentifyIgnoreQueryShape) == true)
                    {
                        queryFilter.QueryShape = null;
                    }

                    if (isMultiQuery)
                    {
                        tasks.Add(serviceQuery, serviceQuery.HasFeaturesAsync(bridge.RequestContext, queryFilters[serviceQuery],
                                                                              appendFilterClause: filterWhereClause,
                                                                              mapScale: eventScale));
                    }
                    else
                    {
                        features = await serviceQuery.PerformAsync(bridge.RequestContext, queryFilters[serviceQuery],
                                                                   appendFilterClause: filterWhereClause,
                                                                   limit: Math.Max(serviceQuery.MaxFeatures, e.GetInt(IdentifyDefault.IdentifyFeatureLimiit)),
                                                                   mapScale: eventScale);
                        if (features != null)
                        {
                            found.Add(serviceQuery, features.Count);
                        }
                    }
                }
            }

            #region Collect Tasks

            if (tasks.Count > 0)
            {
                Task.WaitAll(tasks.Values.ToArray());

                foreach (var query in tasks.Keys)
                {
                    if (tasks[query].Result > 0)
                    {
                        found.Add(query, tasks[query].Result);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StringBuilder errorMessage = new StringBuilder();

            errorMessage.Append($"Queries: {String.Join(", ", queries.Select(q => q.Name))}");
            errorMessage.Append("\n\n");
            if (!String.IsNullOrEmpty(filterWhereClause))
            {
                errorMessage.Append($"Filter: {filterWhereClause}");
                errorMessage.Append("\n\n");
            }
            errorMessage.Append(String.Join("\n", ex.Message
                .Replace("(", "\n(")  // every new Exception from Task.WaitAll will be listet in Brackets (....) (...) 
                .Split('\n')
                .Distinct()    // Remove identical lines
                ));

            throw new Exception(errorMessage.ToString(), ex);
        }

        #endregion

        if (e.AsDefaultTool == true &&
            isMultiQuery == true &&
            !String.IsNullOrWhiteSpace(e["identify-map-tools"]) &&
            identifyOptions.Contains("all-identify-tools"))
        {
            #region Query all other "IdentiyTools" also

            string[] availableServiceIds = e["identify-all-services"]?.Split(';');
            string[] availableQueryIds = e["identify-all-queries"]?.Split(';');

            foreach (var toolId in e["identify-map-tools"].Split(','))
            {
                var button = bridge.TryGetFriendApiButton(tool, toolId);
                if (button is IIdentifyTool && (button is IApiServerTool || button is IApiServerToolAsync))
                {
                    var canIdentifyResults = await ((IIdentifyTool)button).CanIdentifyAsync(bridge, new Point(click.Longitude, click.Latitude), eventScale, availableServiceIds, availableQueryIds);
                    if (canIdentifyResults != null)
                    {
                        foreach (var canIdentifyResult in canIdentifyResults)
                        {
                            var toolQuery = new IdentifyToolQuery(bridge, (IApiTool)button, toolId, canIdentifyResult.Name, canIdentifyResult.ToolParameters);
                            found.Add(toolQuery, canIdentifyResult.Count);

                            var queryFilter = (ApiSpatialFilter)filter.Clone();
                            queryFilter.FeatureSpatialReference = bridge.GetSupportedSpatialReference(toolQuery, bridge.DefaultSrefId);
                            queryFilters.Add(toolQuery, queryFilter);
                        }
                    }
                }
            }

            #endregion
        }

        if (e.AsDefaultTool == true &&
            isMultiQuery == true &&
            found.Keys.Count == 1 &&
            hasExcludedQueries == false &&
            queryTheme.EndsWith("~") == false)  // nicht sichtbare Themen (~) ausschließen => Immer Liste anzeigen
        {
            var query = found.Keys.First();
            if (!(query is IdentifyToolQuery))
            {
                found.Clear();
                isMultiQuery = false;

                features = await query.PerformAsync(bridge.RequestContext, queryFilters[query]);
                if (features != null)
                {
                    found.Add(query, features.Count);
                }

                zoom2results = false;
            }
        }

        #endregion

        if (isMultiQuery)
        {
            #region Multiple Queries available => Build Select Query MenuItems

            #region MenuItems

            List<UIElement> menuItems = new List<UIElement>();

            foreach (var query in found.Keys.OrderByFavorites(favItems)
                                            .Where(q => !(q is IdentifyToolQuery)))
            {
                int count = found[query];

                var legendImageUrl = queryFilters.ContainsKey(query) ?
                    await query.LegendItemImageUrlAsync(bridge.RequestContext, queryFilters[query]) :
                    String.Empty;
                var text = query.Name + (count > 0 ? "&nbsp;[" + count + "]" : "");
                var value = query is IdentifyToolQuery ? ((IdentifyToolQuery)query).Url : bridge.GetQueryThemeId(query);
                var icon = !String.IsNullOrWhiteSpace(legendImageUrl) ? bridge.AppRootUrl + legendImageUrl : null;

                if (addCheckboxes)
                {
                    menuItems.Add(new UIMenuItemCheckable(tool, e)
                    {
                        text = text,
                        value = value,
                        icon = icon
                    });
                }
                else
                {
                    menuItems.Add(new UIMenuItem(tool, e)
                    {
                        text = text,
                        value = value,
                        icon = icon
                    });
                }
            }

            #endregion

            Dictionary<string, UICollapsableElement> categoryElements = new Dictionary<string, UICollapsableElement>();

            if (hasExcludedQueries)
            {
                #region Some Queries are excluded because of user favorites

                if (queryTheme.IsFavoritesRemovedQueryThemeType())
                {
                    if (menuItems.Count == 0 && e.HasAvoidRecursiveRecallsFlag() == false)
                    {
                        e[IdentifyDefault.IdentifyQueryThemeId] = queryTheme.ToIgnoreFavoritesTypeQueryTheme();
                        e.AddAvoidRecursiveRecallsFlag();

                        e.ClearMenuItemValue();

                        return await OnEvent(tool, bridge, e);
                    }
                }
                else if (queryTheme.IsDefaultQueryThemeType())
                {
                    if (menuItems.Count == 0 && e.HasAvoidRecursiveRecallsFlag() == false)
                    {
                        e[IdentifyDefault.IdentifyQueryThemeId] = queryTheme.ToIgnoreFavoritesTypeQueryTheme();
                        e.AddAvoidRecursiveRecallsFlag();

                        e.ClearMenuItemValue();

                        return await OnEvent(tool, bridge, e);
                    }

                    if (!appendToUI)
                    {
                        targetCategoryId = categoryElements.AddFavoritesCategory(tool, e);
                        if (queryTheme == IdentifyConst.QueryAllDefault)
                        {
                            categoryElements.AddAllCategory(tool, e, true, queryTheme.ToRemoveFavoritesTypeQueryTheme().AddAppendToUIFlag());
                        }
                        else
                        {
                            categoryElements.AddVisibleCategory(tool, e, queryTheme.ToRemoveFavoritesTypeQueryTheme().AddAppendToUIFlag());
                            categoryElements.AddInVisibleCategory(tool, e, IdentifyConst.QueryInvisibleIgnoreFavorites.AddAppendToUIFlag());
                        }
                    }
                }

                #endregion
            }
            else
            {
                if (!appendToUI)
                {
                    if (queryTheme.IsAllInOneQueryThemeType())
                    {
                        targetCategoryId = categoryElements.AddAllCategory(tool, e, false, queryTheme.ToRemoveFavoritesTypeQueryTheme().AddAppendToUIFlag());
                    }
                    else
                    {
                        targetCategoryId = categoryElements.AddVisibleCategory(tool, e);
                        categoryElements.AddInVisibleCategory(tool, e, IdentifyConst.QueryInvisibleIgnoreFavorites.AddAppendToUIFlag());
                    }
                }
            }

            if (e.AsDefaultTool && !appendToUI)
            {
                categoryElements.AddIdentifyToolCategory(found.Keys.Where(q => q is IdentifyToolQuery)
                                                                   .Select(q => (IdentifyToolQuery)q),
                                                         bridge, tool, e);
            }

            response = new ApiEventResponse()
            {
                UIElements = categoryElements.ToUIElements(
                        menuItems,
                        targetCategoryId,
                        targetElementId: e[IdentifyDefault.IdentifyMultiResultTarget].OrTake(
                                            e.AsDefaultTool
                                                ? UIElementTarget.tool_sidebar_top.ToString()
                                                : UIElementTarget.tool_persistent_topic.ToString()),
                        targetElementTitle: "Aktuelle Auswahl: Treffer"
                    ),
                UISetters = new List<IUISetter>()
            };

            switch (e["_method"])
            {
                case "box":
                    response.ToolCursor = ToolCursor.Custom_Rectangle;
                    break;
                case "apply":
                    response.ToolCursor = ToolCursor.Custom_Pen;
                    break;
            }

            #endregion
        }
        else if (found.Count == 1)
        {
            var performedQuery = found.Keys.First();

            ICollection<IUIElement> uiElements = null;
            if (!e.AsDefaultTool && String.IsNullOrEmpty(e.MenuItemValue))
            {
                // clear topic area, if Query not triggered by a menu Item
                uiElements = new IUIElement[] { new UIEmpty() { target = UIElementTarget.tool_persistent_topic.ToString() } };
            }

            response = new ApiFeaturesEventResponse()
            {
                UISetters = new List<IUISetter>(),
                CustomSelectionId = bridge.IdentifyCustomSelection(queryShape, click.SRef, e),
                UIElements = uiElements
            }
                .AddFeatures(features, FeatureResponseType.New, appendHoverShapes: true)
                .AddFeaturesQuery(performedQuery)
                .AddFeaturesFilter(queryFilters.ContainsKey(performedQuery) ? queryFilters[performedQuery] : filter)
                .AddFeaturesSpatialReference(queryFilters[performedQuery].FeatureSpatialReference)
                .AddClickEvent(click)
                .SelectFeaturesResult(!e.AsDefaultTool && performedQuery.IsSelectable)
                .ZoomToFeaturesResult(zoom2results);

            SetPointerTool(response);
        }
        else
        {
            response = new ApiEventResponse();
        }

        if (response.UISetters != null)
        {
            response.UISetters.Add(new UISetter("identify-tool-selection-method", e["_method"]));
        }

        if (errors.Length > 0)
        {
            response.ErrorMessage = errors.ToString();
        }

        if (e["_method"] == "box" && found.Count != 1)
        {
            SetPointerTool(response);
        }

        return response;
    }

    #region Helper

    private void SetPointerTool(ApiEventResponse response)
    {
        response
            .SetActiveToolType(ToolType.click)
            .SetActiveToolCursor(ToolCursor.Custom_Pan_Info)
            .AddUISetters(
                new UICssSetter(UICssSetter.SetterType.SelectOption, "webgis-identify-tool", "pointer"),
                new UICssSetter(UICssSetter.SetterType.AddClass, IdentifyDefault.SketchButtonContainerId, UICss.HiddenUIElement),
                new UICssSetter(UICssSetter.SetterType.AddClass, IdentifyDefault.SketchBufferContainerId, UICss.HiddenUIElement),
                new UISetter(IdentifyDefault.SketchCanApplyBufferId, "false"));
    }

    #endregion
}
