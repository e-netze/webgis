using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class ApiEventResponseExtensions
{
    #region UIElements

    static public T UIElementsBehavoir<T>(this T eventResponse, AppendUIElementsMode mode)
        where T : ApiEventResponse
    {
        switch (mode)
        {
            case AppendUIElementsMode.Append:
                eventResponse.AppendUIElements = true;
                break;
            default:
                eventResponse.AppendUIElements = null;
                break;
        }

        return eventResponse;
    }

    static public T AddUIElement<T>(this T eventResponse, IUIElement element, out IUIElement addedElement)
        where T : ApiEventResponse
    {
        if (eventResponse.UIElements == null)
        {
            eventResponse.UIElements = new List<IUIElement>();
        }

        eventResponse.UIElements.Add(addedElement = element);

        return eventResponse;
    }

    static public T AddUIElement<T>(this T eventResponse, IUIElement element)
        where T : ApiEventResponse
    {
        return AddUIElement(eventResponse, element, out IUIElement addedElement);
    }

    static public T AddUIElements<T>(this T eventResponse, params IUIElement[] elements)
        where T : ApiEventResponse
    {
        if (eventResponse.UIElements == null)
        {
            eventResponse.UIElements = new List<IUIElement>();
        }
        else if (!(eventResponse.UIElements is List<IUIElement>))
        {
            eventResponse.UIElements = new List<IUIElement>(eventResponse.UIElements);
        }

        ((List<IUIElement>)eventResponse.UIElements).AddRange(elements);

        return eventResponse;
    }

    static public T AddUISetter<T>(this T eventResponse, IUISetter setter)
        where T : ApiEventResponse
    {
        if (eventResponse.UISetters == null)
        {
            eventResponse.UISetters = new List<IUISetter>();
        }

        eventResponse.UISetters.Add(setter);

        return eventResponse;
    }

    static public T AddUISetters<T>(this T eventResponse, params IUISetter[] setters)
        where T : ApiEventResponse
    {
        if (eventResponse.UISetters == null)
        {
            eventResponse.UISetters = new List<IUISetter>();
        }
        else if (!(eventResponse.UIElements is List<IUISetter>))
        {
            eventResponse.UISetters = new List<IUISetter>(eventResponse.UISetters);
        }

        ((List<IUISetter>)eventResponse.UISetters).AddRange(setters);

        return eventResponse;
    }

    static public T EmptyUIElement<T>(this T eventResponse, UIElementTarget target)
        where T : ApiEventResponse
    {
        return eventResponse.AddUIElement(new UIEmpty()
                                              .WithTarget(target));
    }

    static public T EmptyUIElement<T>(this T eventResponse, string target)
        where T : ApiEventResponse
    {
        return eventResponse.AddUIElement(new UIEmpty()
                                              .WithTarget(target));
    }

    static public T CloseUIDialog<T>(this T eventResponse, UIElementTarget target = UIElementTarget.modaldialog)
        where T : ApiEventResponse
        => eventResponse.EmptyUIElement(target);

    #endregion

    #region Sketch

    static public T AddVertexToCurrentSketch<T>(this T eventResponse, Point vertex)
        where T : ApiEventResponse
    {
        if (eventResponse != null && vertex != null)
        {
            eventResponse.SketchAddVertex = vertex;
        }

        return eventResponse;
    }

    static public T AddNamedSketches<T>(this T eventResponse, IEnumerable<NamedSketch> namedSketches)
        where T : ApiEventResponse
    {
        if (eventResponse != null && namedSketches != null)
        {
            if (eventResponse.NamedSketches != null)  // append to existing
            {
                eventResponse.NamedSketches = new List<NamedSketch>(eventResponse.NamedSketches);
                ((List<NamedSketch>)eventResponse.NamedSketches).AddRange(namedSketches);
            }
            else
            {
                // Make a list or array
                // => if enumerable, it will be called serveral times during serialization to the client
                // often with in the emum pipiline is also coords projection. That would be run servarl
                // times also!
                eventResponse.NamedSketches = namedSketches.ToArray();
            }
        }

        return eventResponse;
    }

    #endregion

    #region Features 

    static public T AddFeatures<T>(this T eventResponse,
                                   FeatureCollection features,
                                   FeatureResponseType featureReponseType,
                                   bool appendHoverShapes = false)
        where T : ApiFeaturesEventResponse
    {
        if (eventResponse != null && features != null)
        {
            eventResponse.Features = features;
            eventResponse.FeatureResponseType = featureReponseType;
            eventResponse.AppendHoverShapes = appendHoverShapes;
        }

        return eventResponse;
    }

    static public T AddFeaturesForLinks<T>(this T eventResponse,
                                           FeatureCollection features)
        where T : ApiFeaturesEventResponse
    {
        if (eventResponse != null && features != null)
        {
            eventResponse.FeaturesForLinks = features;
        }

        return eventResponse;
    }

    static public T AddFeaturesSpatialReference<T>(this T eventResponse,
                                                   SpatialReference sRef)
        where T : ApiFeaturesEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.FeatureSpatialReference = sRef;
        }

        return eventResponse;
    }

    static public T AddFeaturesFilter<T>(this T eventResponse,
                                         ApiQueryFilter filter)
        where T : ApiFeaturesEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.Filter = filter;
        }

        return eventResponse;
    }

    static public T AddFeaturesQuery<T>(this T eventResponse,
                                        IQueryBridge query)
        where T : ApiFeaturesEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.Query = query;
        }

        return eventResponse;
    }

    static public T ZoomToFeaturesResult<T>(this T eventResponse, bool zoom = true)
        where T : ApiFeaturesEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.ZoomToResults = zoom;
        }

        return eventResponse;
    }

    static public T SelectFeaturesResult<T>(this T eventResponse, bool select = true)
        where T : ApiFeaturesEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.SelectResults = select;
        }

        return eventResponse;
    }

    #endregion

    #region Lense

    static public T AddMapViewLense<T>(this T eventResponse, Lense lense)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.MapViewLense = lense;
        }

        return eventResponse;
    }

    #endregion

    #region Navigation

    static public T SetMapScale<T>(this T eventResponse, double scale)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.ZoomToScale = scale;
        }

        return eventResponse;
    }

    static public T SetMapBbox4326<T>(this T eventResponse, double[] bbox4326)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.ZoomTo4326 = bbox4326;
        }

        return eventResponse;
    }

    #endregion

    #region Tools / Cursors

    static public T SetActiveToolType<T>(this T eventResponse, ToolType activeToolType)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.ActiveToolType = activeToolType;
        }

        return eventResponse;
    }

    static public T SetActiveTool<T>(this T eventResponse, IApiButton activeTool)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.ActiveTool = activeTool;
        }

        return eventResponse;
    }

    static public T SetActiveToolCursor<T>(this T eventResponse, ToolCursor toolCursor)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.ToolCursor = toolCursor;
        }

        return eventResponse;
    }

    #endregion

    #region Events / Responses / Commands

    static public T AddClickEvent<T>(this T eventResponse, ApiToolEventArguments.ApiToolEventClick clickEvent)
        where T : ApiFeaturesEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.ClickEvent = clickEvent;
        }

        return eventResponse;
    }

    static public T AddGraphicsResponse<T>(this T eventResponse, GraphicsResponse graphcis)
        where T : ApiEventResponse
    {
        if (eventResponse != null && graphcis != null)
        {
            eventResponse.Graphics = graphcis;
        }

        return eventResponse;
    }

    static public T AddClientCommands<T>(this T eventResponse, params ApiClientButtonCommand[] clientCommands)
        where T : ApiEventResponse
    {
        if (eventResponse.ClientCommands == null)
        {
            eventResponse.ClientCommands = new List<ApiClientButtonCommand>();
        }
        else if (!(eventResponse.UIElements is List<ApiClientButtonCommand>))
        {
            eventResponse.ClientCommands = new List<ApiClientButtonCommand>(eventResponse.ClientCommands);
        }

        ((List<ApiClientButtonCommand>)eventResponse.ClientCommands).AddRange(clientCommands);

        return eventResponse;
    }

    static public T AddClientCommandData<T>(this T eventResponse, object data)
         where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.ClientCommandData = data;
        }

        return eventResponse;
    }

    #endregion

    #region Services / Selection / Layers / Filters / Snapping

    static public T DoRefreshServices<T>(this T eventResponse, params string[] serviceIds)
        where T : ApiEventResponse
        => eventResponse.DoRefreshServices((IEnumerable<string>)serviceIds);

    static public T DoRefreshServices<T>(this T eventResponse, IEnumerable<string> serviceIds)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.RefreshServices =
                    eventResponse.RefreshServices != null ?
                    eventResponse.RefreshServices.Concat(serviceIds).ToArray() :
                    serviceIds.ToArray();
        }

        return eventResponse;
    }

    static public T DoRefreshSelection<T>(this T eventResponse, bool refresh = true)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.RefreshSelection = refresh;
        }

        return eventResponse;
    }

    static public T DoRefreshSnapping<T>(this T eventResponse, bool refresh = true)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.RefreshSnapping = refresh;
        }

        return eventResponse;
    }

    static public T AddFilters<T>(this T eventResponse, params FilterDefintionDTO[] filters)
        where T : ApiEventResponse
        => eventResponse.AddFilters((IEnumerable<FilterDefintionDTO>)filters);

    static public T AddFilters<T>(this T eventResponse, IEnumerable<FilterDefintionDTO> filters)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.SetFilters = filters.ToArray();
        }

        return eventResponse;
    }

    static public T RemoveFilters<T>(this T eventResponse, params FilterDefintionDTO[] filters)
        where T : ApiEventResponse
        => eventResponse.RemoveFilters((IEnumerable<FilterDefintionDTO>)filters);

    static public T RemoveFilters<T>(this T eventResponse, IEnumerable<FilterDefintionDTO> filters)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.UnsetFilters = filters.ToArray();
        }

        return eventResponse;
    }

    static public T AddLayerVisibility<T>(this T eventResponse, LayerVisibility layerVisiblity)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.SetLayerVisility = layerVisiblity;
        }

        return eventResponse;
    }

    static public T AddLabeling<T>(this T eventResponse, params LabelingDefinitionDTO[] labeling)
        where T : ApiEventResponse
        => eventResponse.AddLabeling((IEnumerable<LabelingDefinitionDTO>)labeling);

    static public T AddLabeling<T>(this T eventResponse, IEnumerable<LabelingDefinitionDTO> labeling)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.SetLabeling = labeling.ToArray();
        }

        return eventResponse;
    }

    static public T RemoveLabeling<T>(this T eventResponse, params LabelingDefinitionDTO[] labeling)
        where T : ApiEventResponse
        => eventResponse.RemoveLabeling((IEnumerable<LabelingDefinitionDTO>)labeling);

    static public T RemoveLabeling<T>(this T eventResponse, IEnumerable<LabelingDefinitionDTO> labeling)
        where T : ApiEventResponse
    {
        if (eventResponse != null)
        {
            eventResponse.UnsetLabeling = labeling.ToArray();
        }

        return eventResponse;
    }

    #endregion
}
