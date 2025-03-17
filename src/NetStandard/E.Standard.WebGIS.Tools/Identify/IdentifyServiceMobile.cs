using E.Standard.ThreadSafe;
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
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static E.Standard.WebMapping.Core.CoreApiGlobals;

namespace E.Standard.WebGIS.Tools.Identify;

internal class IdentifyServiceMobile
{
    async public Task<ApiEventResponse> OnEvent(IApiTool tool, IBridge bridge, ApiToolEventArguments e)
    {
        var click = e.ToMapProjectedClickEvent();

        double mapScale = e.GetDouble("identify-map-scale");
        string allQueries = e["identify-all-queries"];


        double[] mapBBox = e.GetDoubleArray("identify-map-bbox");
        int[] mapSize = e.GetArray<int>("identify-map-size");
        string[] identifyOptions = e["identify-tool-options"].TryParseJson<string[]>(new string[0]);
        bool addCheckboxes = e.GetConfigBool("show-layer-visibility-checkboxes");

        ApiEventResponse response = null;

        IEnumerable<IQueryBridge> queries = null;
        bool isMultiQuery = true, zoom2results = false, hasExcludedQueries = false;

        #region Get QueryTheme Name

        string queryTheme = e.MenuItemValue;

        if (String.IsNullOrEmpty(queryTheme))
        {
            queryTheme = e["identify-query-theme"] ?? String.Empty;

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

        #endregion

        #region Collect Queries

        var favItems = await bridge.GetUserFavoriteItemsAsync(tool, "OnEvent");

        if (queryTheme == IdentifyConst.QueryVisibleDefault)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Visible))
                .TakeFavorites(favItems, out hasExcludedQueries);
        }
        else if (queryTheme == IdentifyConst.QueryVisibleRemoveFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Visible))
                .RemoveFavorites(favItems, out hasExcludedQueries);
        }
        else if (queryTheme == IdentifyConst.QueryVisibleIgnoreFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Visible));
        }
        else if (queryTheme == IdentifyConst.QueryInvisibleDefault)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Invisible))
                .TakeFavorites(favItems, out hasExcludedQueries);
        }
        else if (queryTheme == IdentifyConst.QueryInvisibleRemoveFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Invisible))
                .RemoveFavorites(favItems, out hasExcludedQueries);
        }
        else if (queryTheme == IdentifyConst.QueryInvisibleIgnoreFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Invisible));
        }
        else if (queryTheme == IdentifyConst.QueryAllDefault)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Any))
                .TakeFavorites(favItems, out hasExcludedQueries);
        }
        else if (queryTheme == IdentifyConst.QueryAllRemoveFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Any))
                .RemoveFavorites(favItems, out hasExcludedQueries);
        }
        else if (queryTheme == IdentifyConst.QueryAllIgnoreFavorites)
        {
            queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Any));
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
            throw new Exception("Keine Abfragen gefunden!?");
        }

        #region Add Map Sref, BBox & Size (wichtig für WMS Identify) 

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

        ThreadSafeDictionary<IQueryBridge, int> found = new ThreadSafeDictionary<IQueryBridge, int>();
        ThreadSafeDictionary<IQueryBridge, ApiSpatialFilter> queryFilters = new ThreadSafeDictionary<IQueryBridge, ApiSpatialFilter>();
        WebMapping.Core.Collections.FeatureCollection features = null;

        StringBuilder errors = new StringBuilder();

        #region Perform Queries

        if (queries != null)
        {
            List<Task<int>> tasks = new List<Task<int>>();
            foreach (var query in queries)
            {
                try
                {
                    var queryFilter = (ApiSpatialFilter)filter.Clone();
                    queryFilter.FeatureSpatialReference = bridge.GetSupportedSpatialReference(query, bridge.DefaultSrefId);
                    queryFilters.Add(query, queryFilter);

                    if (queryShape == null)
                    {
                        #region Click Tolerance

                        double pixelTolerance = e.IdentifyTolerance(query);

                        if (pixelTolerance == 0.0)
                        {
                            queryFilter.QueryShape = new Point(click.WorldX, click.WorldY);
                        }
                        else
                        {
                            double toleranceX, toleranceY;
                            toleranceX = toleranceY = pixelTolerance * mapScale / (96.0 / 0.0254);
                            if (click.SRef.IsProjective == false)
                            {
                                toleranceX = toleranceX * ToDeg / WorldRadius * Math.Cos(click.Latitude * ToRad);
                                toleranceY = toleranceY * ToDeg / WorldRadius;
                            }
                            queryFilter.QueryShape = new Envelope(
                                click.WorldX - toleranceX, click.WorldY - toleranceY,
                                click.WorldX + toleranceX, click.WorldY + toleranceY);
                        }

                        #endregion
                    }
                    else
                    {
                        queryFilter.QueryShape = queryShape;
                    }

                    if (isMultiQuery)
                    {
                        int count = await query.HasFeaturesAsync(bridge.RequestContext, queryFilters[query], mapScale: mapScale);
                        if (count > 0)
                        {
                            found.Add(query, count);
                        }
                    }
                    else
                    {
                        features = await query.PerformAsync(bridge.RequestContext, queryFilters[query], limit: query.MaxFeatures, mapScale: mapScale);
                        if (features != null)
                        {
                            found.Add(query, features.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.AppendErrorMessage(query.Name, ex);
                }
            }

            if (e.AsDefaultTool == true && isMultiQuery == true && !String.IsNullOrWhiteSpace(e["identify-map-tools"]) && identifyOptions.Contains("all-identify-tools"))
            {
                #region Query all other "IdentiyTools" also

                string[] availableServiceIds = e["identify-all-services"]?.Split(';');
                string[] availableQueryIds = e["identify-all-queries"]?.Split(';');

                foreach (var toolId in e["identify-map-tools"].Split(','))
                {
                    var button = bridge.TryGetFriendApiButton(tool, toolId);
                    if (button is IIdentifyTool && (button is IApiServerTool || button is IApiServerToolAsync))
                    {
                        var canIdentifyResults = await ((IIdentifyTool)button).CanIdentifyAsync(bridge, new Point(click.Longitude, click.Latitude), mapScale, availableServiceIds, availableQueryIds);
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
                found.AllKeys.Count == 1 &&
                hasExcludedQueries == false &&
                queryTheme.EndsWith("~") == false)  // nicht sichtbare Themen (~) ausschließen => Immer Liste anzeigen
            {
                var query = found.AllKeys[0];
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
        }

        #endregion

        if (isMultiQuery)
        {
            #region Multiple Queries available => Build Select Query MenuItems

            List<UIElement> menuItems = new List<UIElement>();
            foreach (var query in found.Keys.OrderByFavorites(favItems))
            {
                int count = found[query];

                if (query is IdentifyToolQuery)
                {
                    menuItems.Add(new UIMenuItem(tool, e)
                    {
                        text = query.Name + (count > 0 ? "&nbsp;[" + count + "]" : ""),
                        value = ((IdentifyToolQuery)query).Url,
                        icon = !String.IsNullOrWhiteSpace(((IdentifyToolQuery)query).Image) ? bridge.AppRootUrl + "/" + ((IdentifyToolQuery)query).Image : null
                    });
                }
                else
                {
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
            }

            #endregion

            if (hasExcludedQueries)
            {
                #region Some Queries are excluded because of user favorites

                if (queryTheme.IsFavoritesRemovedQueryThemeType())
                {
                    if (menuItems.Count == 0 && String.IsNullOrEmpty(e["_avoid_recursive_recalls"]))
                    {
                        e["identify-query-theme"] = queryTheme.ToIgnoreFavoritesTypeQueryTheme();
                        e["_avoid_recursive_recalls"] = "true";

                        e.ClearMenuItemValue();

                        return await OnEvent(tool, bridge, e);
                    }
                    menuItems.Add(new UIMenuItem(tool, e)
                    {
                        text = "Favoriten...",
                        value = queryTheme.ToDefaultTypeQueryTheme(),
                    });
                    if (addCheckboxes)
                    {
                        menuItems.Add(new UIMenuItem(tool, e)
                        {
                            text = "Nicht sichtbare Themen abfragen...",
                            value = IdentifyConst.QueryInvisibleIgnoreFavorites,
                        });
                    }
                }
                else if (queryTheme.IsDefaultQueryThemeType())
                {
                    if (menuItems.Count == 0 && String.IsNullOrEmpty(e["_avoid_recursive_recalls"]))
                    {
                        e["identify-query-theme"] = queryTheme.ToIgnoreFavoritesTypeQueryTheme();
                        e["_avoid_recursive_recalls"] = "true";

                        e.ClearMenuItemValue();

                        return await OnEvent(tool, bridge, e);
                    }
                    menuItems.Add(new UIMenuItem(tool, e)
                    {
                        text = "Weitere (sichtbare) Ergebnisse...",
                        value = queryTheme.ToRemoveFavoritesTypeQueryTheme(),
                    });
                }

                #endregion
            }
            else
            {
                if (addCheckboxes && queryTheme == IdentifyConst.QueryVisibleDefault)
                {
                    menuItems.Add(new UIMenuItem(tool, e)
                    {
                        text = "Nicht sichtbare Themen abfragen...",
                        value = IdentifyConst.QueryInvisibleIgnoreFavorites,
                    });
                }
            }

            response = new ApiEventResponse()
            {
                UIElements = new IUIElement[] {
                    new UIMenu()
                    {
                        elements = menuItems.ToArray(),
                        target = UIElementTarget.modaldialog.ToString(),
                        header = menuItems.Count > 0 ? "Abfrage Ergebnisse" + ((hasExcludedQueries && !queryTheme.StartsWith(".")) ? " (Favoriten)" : "") : "Keine Abfrageergebnisse gefunden"
                    }
                },
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
        }
        else if (found.Count == 1)
        {
            var performedQuery = found.AllKeys[0];

            response = new ApiFeaturesEventResponse()
            {
                Features = features,
                Query = performedQuery,
                Filter = queryFilters.ContainsKey(performedQuery) ? queryFilters[performedQuery] : filter,
                FeatureSpatialReference = queryFilters[performedQuery].FeatureSpatialReference,
                ZoomToResults = zoom2results,
                ClickEvent = click,
                SelectResults = !e.AsDefaultTool && performedQuery.IsSelectable,
                UISetters = new List<IUISetter>(),
                CustomSelectionId = bridge.IdentifyCustomSelection(queryShape, click.SRef, e)
            };
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

        if (e["identify-tool-selection-method"] == "box" || e["_method"] == "box")
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
