using E.Standard.Drawing.Models;
using E.Standard.Extensions.Collections;
using E.Standard.Extensions.Compare;
using E.Standard.Localization.Abstractions;
using E.Standard.Localization.Reflection;
using E.Standard.ThreadSafe;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Identify.Extensions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Reflection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using static E.Standard.WebMapping.Core.CoreApiGlobals;

namespace E.Standard.WebGIS.Tools.Identify;

[Export(typeof(IApiButton))]
[ToolId("webgis.tools.identify")]
[AdvancedToolProperties(VisFilterDependent = true,
                        AllowCtrlBBox = true,
                        CustomServiceRequestParametersDependent = true,
                        ClientDeviceDependent = true,
                        MapCrsDependent = true,
                        UIElementDependency = true,
                        MapBBoxDependent = true)]
[ToolConfigurationSection("identify")]
[ToolHelp("tools/identify/identify.html", urlPathDefaultTool: "index.html")]
[LocalizationNamespace("tools.identify")]
public class IdentifyDefault : IApiServerToolLocalizableAsync<IdentifyDefault>,
                               IApiButtonDependency,
                               IApiButtonResources,
                               IApiToolMarker,
                               IAdvancedSketchTool
{
    public const string IdentifyAllQueriesId = "identify-all-queries";
    public const string IdentifyAllServicesId = "identify-all-services";
    public const string IdentifyMapScaleId = "identify-map-scale";
    public const string IdentifyMapBBoxId = "identify-map-bbox";
    public const string IdentifyMapSizeId = "identify-map-size";
    public const string IdentifyMapToolsId = "identify-map-tools";
    public const string IdentifyToolOptionsId = "identify-tool-options";
    public const string IdentifyToolSelectionMethodId = "identify-tool-selection-method";
    public const string IdentifyQueryThemeId = "identify-query-theme";
    public const string WebGisIdentifyToolId = "webgis-identify-tool";
    public const string IdentifyMultiResultTarget = "webgis-identify-multi-result-target";
    public const string IdentifyWhereClause = "webgis-identify-where";
    public const string IdentifyForceCheckboxes = "webgis-identify-forece-checkboxex";
    public const string IdentifyUiPrefix = "webgis-identify-ui-prefix";

    public const string IdentifyFeatureLimiit = "webgis-identify-feature-limit";
    public const string IdentifyIgnoreQueryShape = "webgis-identify-ignore-query-shame";

    internal const string SketchButtonContainerId = "webgis-identify-sketch-buttons-container";
    internal const string SketchBufferContainerId = "webgis-identify-sketch-buffer-container";
    internal const string SketchBufferDistanceId = "webgis-identify-sketch-buffer-distance";
    internal const string SketchBufferUnitId = "webgis-identify-sketch-buffer-unit";
    internal const string SketchCanApplyBufferId = "webgis-identify-sketch-can-apply-buffer";

    public const string ShowLayerVisibilityCheckboxesConfigKey = "show-layer-visibility-checkboxes";

    #region IApiServerTool Member

    async public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<IdentifyDefault> localizer)
    {
        List<UINameValue> comboCustomItems = new List<UINameValue>(new UINameValue[]{
                        new UINameValue(){
                            name = localizer.Localize("visible-layers"), value="#"
                        },
                        new UINameValue(){
                            name = localizer.Localize("all-layers"), value="*"
                        }
                    });


        var favItems = await bridge.GetUserFavoriteItemsAsync(this, "OnEvent");
        foreach (var favItem in favItems)
        {
            try
            {
                var query = await bridge.GetQuery(favItem.Split(':')[0], favItem.Split(':')[1]);
                if (query != null)
                {
                    //var service = await bridge.GetService(favItem.Split(':')[0]);

                    comboCustomItems.Add(new UINameValue()
                    {
                        name = query.Name /*+ (service != null ? " (" + service.Name + ") : "")"*/,
                        value = favItem,
                        category = localizer.Localize("favorites")
                    });
                }
            }
            catch { }
        }

        List<IUISetter> uiSetters = new List<IUISetter>();
        List<IUIElement> uiElements = new List<IUIElement>(
            new IUIElement[]{
                new UIHidden(){
                    id= IdentifyAllQueriesId,
                    css=UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.AutoSetterAllQueries })
                },
                new UIHidden()
                {
                    id= IdentifyAllServicesId,
                    css=UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.AutoSetterAllServices })
                },
                new UIHidden(){
                    id= IdentifyMapScaleId,
                    css=UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.AutoSetterMapScale })
                },
                new UIHidden()
                {
                    id= IdentifyMapBBoxId,
                    css=UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.AutoSetterMapBBox })
                },
                new UIHidden()
                {
                    id= IdentifyMapSizeId,
                    css=UICss.ToClass(new string[]{ UICss.ToolParameter, UICss.AutoSetterMapSize })
                },
                new UIHidden()
                {
                    id= IdentifyMapToolsId,
                    css=UICss.ToClass(new string[] { UICss.ToolParameter, UICss.AutoSetterMapTools })
                },
                new UIHidden()
                {
                    id= IdentifyToolOptionsId,
                    css=UICss.ToClass(new string[] { UICss.ToolParameter, UICss.AutoSetterToolOptions })
                },
                new UIHidden
                {
                    id= IdentifyToolSelectionMethodId,
                    css=UICss.ToClass(new string[] { UICss.ToolParameter })
                },
                new UIQueryCombo(UIQueryCombo.ComboType.indentify) {
                    id= IdentifyQueryThemeId,
                    css=UICss.ToClass(e.AsDefaultTool ? new string[]{UICss.ToolParameter, UICss.DontOverrideWithQueryStringParameters } : new string[]{UICss.ToolParameter}),
                    customitems=comboCustomItems.ToArray()
                }
            });

        if (!e.AsDefaultTool)
        {
            uiElements.Add(new UIOptionContainer()
            {
                id = WebGisIdentifyToolId,
                css = UICss.ToClass(new string[] { UICss.OptionContainerWithLabels }),
                elements = new IUIElement[]{
                        new UIImageButton(this.GetType(),"pointer",UIButton.UIButtonType.servertoolcommand,"pointer"){
                            value = "pointer",
                            text = localizer.Localize("point-selection")
                        },
                        new UIImageButton(this.GetType(),"rectangle",UIButton.UIButtonType.servertoolcommand,"rectangle"){
                            value = "rectangle",
                            text = localizer.Localize("box-selection")
                        },
                        new UIImageButton(this.GetType(),"circle",UIButton.UIButtonType.servertoolcommand,"circle"){
                            value = "circle",
                            text = localizer.Localize("circle-selection")
                        },
                        new UIImageButton(this.GetType(),"line",UIButton.UIButtonType.servertoolcommand,"line"){
                            value = "line",
                            text = localizer.Localize("select-by-line")
                        },
                        new UIImageButton(this.GetType(),"polygon",UIButton.UIButtonType.servertoolcommand,"polygon"){
                            value = "polygon",
                            text = localizer.Localize("select-by-polygon")
                        }
                    }
            });

            if (!e.UseMobileBehavior())
            {
                uiElements.Add(new UIDiv()
                {
                    id = SketchBufferContainerId,
                    css = UICss.ToClass(new string[] { UICss.HiddenUIElement }),
                    elements = new UIElement[]
                    {
                        new UIHidden()
                        {
                            id = SketchCanApplyBufferId,
                            value = "false",
                            css = UICss.ToClass(new[]{ UICss.ToolParameter })
                        },
                        new UIInputNumber()
                        {
                            id = SketchBufferDistanceId,
                            value = e.GetDouble(SketchBufferDistanceId),
                            css = UICss.ToClass(new[]{ UICss.ToolParameter, UICss.ToolParameterPersistent }),
                            placeholder = localizer.Localize("buffer-distance")
                        },
                        new UISelect()
                        {
                            id = SketchBufferUnitId,
                            css = UICss.ToClass(new[]{ UICss.ToolParameter, UICss.ToolParameterPersistent }),
                            options = new[]
                            {
                                new UISelect.Option() { label = localizer.Localize("meters"), value = "m" },
                                new UISelect.Option() { label = localizer.Localize("kilometers"), value = "km" }
                            }
                        }
                    }
                });

                uiSetters.Add(new UISetter(SketchBufferUnitId, e[SketchBufferUnitId].OrTake("m")));
            }

            uiElements.Add(new UIDiv()
            {
                id = SketchButtonContainerId,
                css = UICss.ToClass(new string[] { UICss.HiddenUIElement }),
                elements = new UIElement[]
                {
                    new UIButtonContainer()
                    {
                        elements = new IUIElement[]
                        {
                            new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removesketch)
                            {
                                text = localizer.Localize("remove-sketch"),
                                 css = UICss.ToClass(new string[]{UICss.CancelButtonStyle})
                            },
                            new UIButton(UIButton.UIButtonType.servertoolcommand, "apply")
                            {
                                text = localizer.Localize("apply"),
                                 css = UICss.ToClass(new string[]{UICss.DefaultButtonStyle})
                            }
                        }
                    }
                }
            });
        }

        if (e.UseDesktopBehavior())
        {
            uiElements.Add(new UIToolPersistentTopic(this));
        }

        return new ApiEventResponse()
        {
            UIElements = uiElements.ToArray(),
            UISetters = uiSetters.ToArray()
        };
    }

    [ServerToolCommand("box")]
    [ServerToolCommand("apply")]
    public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<IdentifyDefault> localizer)
    {
        if (e.UseDesktopBehavior())
        {
            return new IdentifyServiceDesktop().OnEvent(this, bridge, e, localizer);
        }

        return new IdentifyServiceMobile().OnEvent(this, bridge, e, localizer);
    }

    #endregion

    #region IApiTool Member

    virtual public WebMapping.Core.Api.ToolType Type
    {
        get { return WebMapping.Core.Api.ToolType.click; }
    }

    public ToolCursor Cursor
    {
        get
        {
            return ToolCursor.Custom_Pan_Info;
        }
    }

    #endregion

    #region IApiButton Member

    public string Name => "Identify/Select";

    public string Container => "Query";
    public string Image => UIImageButton.ToolResourceImage(this, "identify");

    public string ToolTip => "Identify or select geo-objects on the map.";

    virtual public bool HasUI => true;

    #endregion

    #region IApiButtonDependency Member

    public VisibilityDependency ButtonDependencies
    {
        get
        {
            return VisibilityDependency.QueriesExists;
        }
    }

    #endregion

    #region IApiToolMarker

    public ApiMarker Marker
    {
        get
        {
            return new ApiMarker()
            {
                ImageUrl = UIImageButton.ToolResourceImage(this, "identify-marker"),
                ImageSize = new Dimension(25, 41),
                Anchor = new Position(12, 0),
                PopupAnchor = new Position(0, 0)
            };
        }
    }

    #endregion

    #region IAdvancedSketchTool

    public bool SketchOnlyEditableIfToolTabIsActive => true;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("identify", Properties.Resources.identify);
        toolResourceManager.AddImageResource("identify-marker", Properties.Resources.marker_tool_info_w);
        toolResourceManager.AddImageResource("pointer", Properties.Resources.pointer);
        toolResourceManager.AddImageResource("rectangle", Properties.Resources.rectangle);
        toolResourceManager.AddImageResource("circle", Properties.Resources.circle);
        toolResourceManager.AddImageResource("line", Properties.Resources.polyline);
        toolResourceManager.AddImageResource("polygon", Properties.Resources.polygon);
    }

    #endregion

    #region Commands

    [ServerToolCommand("pointer")]
    public ApiEventResponse OnPointerToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = e.AsDefaultTool ? null : this.Type,
            ToolCursor = ToolCursor.Custom_Pan_Info,
            UISetters = new IUISetter[]
            {
                new UICssSetter(UICssSetter.SetterType.AddClass, SketchButtonContainerId, UICss.HiddenUIElement),
                new UICssSetter(UICssSetter.SetterType.AddClass, SketchBufferContainerId, UICss.HiddenUIElement),
                new UISetter(SketchCanApplyBufferId, "false")
            }
        };
    }

    [ServerToolCommand("rectangle")]
    public ApiEventResponse OnRectangleToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = e.AsDefaultTool ? null : WebMapping.Core.Api.ToolType.box,
            ToolCursor = ToolCursor.Custom_Rectangle,
            UISetters = new IUISetter[]
            {
                new UICssSetter(UICssSetter.SetterType.AddClass, SketchButtonContainerId, UICss.HiddenUIElement),
                new UICssSetter(UICssSetter.SetterType.AddClass, SketchBufferContainerId, UICss.HiddenUIElement),
                new UISetter(SketchCanApplyBufferId, "false")
            }
        };
    }

    [ServerToolCommand("circle")]
    public ApiEventResponse OnCircleToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = e.AsDefaultTool ? null : ToolType.sketchcircle,
            ToolCursor = ToolCursor.Custom_Pen,
            UISetters = new IUISetter[]
            {
                new UICssSetter(UICssSetter.SetterType.RemoveClass, SketchButtonContainerId, UICss.HiddenUIElement),
                new UICssSetter(UICssSetter.SetterType.AddClass, SketchBufferContainerId, UICss.HiddenUIElement),
                new UISetter(SketchCanApplyBufferId, "false")
            }
        };
    }

    [ServerToolCommand("line")]
    public ApiEventResponse OnLineToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = e.AsDefaultTool ? null : WebMapping.Core.Api.ToolType.sketch1d,
            ToolCursor = ToolCursor.Custom_Pen,
            UISetters = new IUISetter[]
            {
                new UICssSetter(UICssSetter.SetterType.RemoveClass, SketchButtonContainerId, UICss.HiddenUIElement),
                new UICssSetter(UICssSetter.SetterType.RemoveClass, SketchBufferContainerId, UICss.HiddenUIElement),
                new UISetter(SketchCanApplyBufferId, "true")
            }
        };
    }

    [ServerToolCommand("polygon")]
    public ApiEventResponse OnPolygonToolClick(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            ActiveToolType = e.AsDefaultTool ? null : WebMapping.Core.Api.ToolType.sketch2d,
            ToolCursor = ToolCursor.Custom_Pen,
            UISetters = new IUISetter[]
            {
                new UICssSetter(UICssSetter.SetterType.RemoveClass, SketchButtonContainerId, UICss.HiddenUIElement),
                new UICssSetter(UICssSetter.SetterType.RemoveClass, SketchBufferContainerId, UICss.HiddenUIElement),
                new UISetter(SketchCanApplyBufferId, "true")
            }
        };
    }

    [ServerToolCommand("sketchfromgeometry")]
    async public Task<ApiEventResponse> OnSketchFromGeometry(IBridge bridge, ApiToolEventArguments e, ILocalizer<IdentifyDefault> localizer)
    {
        int srsId = e.GetInt("identify-srs");
        double X = e.GetDouble("identify-x");
        double Y = e.GetDouble("identify-y");
        string sketchGeometry = e["sketch-geometry"];

        double mapScale = e.GetDouble("identify-map-scale");
        string allQueries = e["identify-all-queries"];

        var queries = (await QueryDefinition.QueriesFromString(bridge, allQueries, QueryDefinition.VisibilityFlag.Visible));

        var sRef = bridge.CreateSpatialReference(srsId);

        #region Filter

        double toleranceX, toleranceY;
        toleranceX = toleranceY = 15.0 * mapScale / (96.0 / 0.0254);
        if (sRef.IsProjective == false)
        {
            toleranceX = toleranceX * ToDeg / WorldRadius * Math.Cos(Y * ToRad);
            toleranceY = toleranceY * ToDeg / WorldRadius;
        }
        var queryShape = new Envelope(
            X - toleranceX, Y - toleranceY,
            X + toleranceX, Y + toleranceY);

        ApiSpatialFilter filter = new ApiSpatialFilter()
        {
            QueryShape = queryShape,
            FilterSpatialReference = sRef,
        };

        #endregion

        ThreadSafeDictionary<IQueryBridge, int> found = new ThreadSafeDictionary<IQueryBridge, int>();

        foreach (var query in queries)
        {
            var layerType = query.GetLayerType();

            switch (sketchGeometry)
            {
                case "polygon":
                    if (layerType != WebMapping.Core.LayerType.polygon)
                    {
                        continue;
                    }

                    break;
                case "hectoline":
                case "dimline":
                case "polyline":
                    if (layerType != WebMapping.Core.LayerType.line)
                    {
                        continue;
                    }

                    break;
                default:
                    continue;
            }

            var count = await query.HasFeaturesAsync(bridge.RequestContext, filter);
            if (count > 0)
            {
                found.Add(query, count);
            }
        }

        List<UIElement> menuItems = new List<UIElement>();
        bool first = true;

        foreach (var query in found.Keys)
        {
            int count = found[query];

            var legendImageUrl = await query.LegendItemImageUrlAsync(bridge.RequestContext, filter);

            var menuItem = new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand_ext, "sketchfromgeometry-get")
            {
                text = query.Name + (count > 0 ? "&nbsp;[" + count + "]" : ""),
                value = query is IdentifyToolQuery ? ((IdentifyToolQuery)query).Url : bridge.GetQueryThemeId(query),
                icon = !String.IsNullOrWhiteSpace(legendImageUrl) ? bridge.AppRootUrl + legendImageUrl : null
            };

            #region Beim ersten Item die Parameter dazuhängen, die dann an die sketchgeometry-get Methode übergeben werden. 
            // Da diese Methode von jedem Sketchtool aufgerufen werden kann, geht das nur so

            if (first)
            {
                menuItem.AddChild(new UIHidden()
                {
                    id = "_identify-srs",
                    css = UICss.ToClass(new string[] { UICss.ToolParameter }),
                    value = srsId.ToString(),
                });
                menuItem.AddChild(new UIHidden()
                {
                    id = "_identify-map-scale",
                    css = UICss.ToClass(new string[] { UICss.ToolParameter }),
                    value = e["identify-map-scale"]
                });
                menuItem.AddChild(new UIHidden()
                {
                    id = "_identify-x",
                    css = UICss.ToClass(new string[] { UICss.ToolParameter }),
                    value = e["identify-x"]
                });
                menuItem.AddChild(new UIHidden()
                {
                    id = "_identify-y",
                    css = UICss.ToClass(new string[] { UICss.ToolParameter }),
                    value = e["identify-y"]
                });
                menuItem.AddChild(new UIHidden()
                {
                    id = "_current-activetool-id",
                    css = UICss.ToClass(new string[] { UICss.ToolParameter }),
                    value = e["current-activetool-id"]
                });

                first = false;
            }

            menuItems.Add(menuItem);

            #endregion
        }

        var response = new ApiEventResponse()
        {
            UIElements = new IUIElement[]{
                new UIMenu()
                    {
                        elements = menuItems.ToArray(),
                        target = UIElementTarget.modaldialog.ToString(),
                        header = menuItems.Count > 0
                            ? localizer.Localize("use-results-from-geometry")
                            : localizer.Localize("no-results-found")
                    }
                },
        };

        return response;
    }

    [ServerToolCommand("sketchfromgeometry-get")]
    async public Task<ApiEventResponse> OnSketchFromGeometryGet(IBridge bridge, ApiToolEventArguments e, ILocalizer<IdentifyDefault> localizer)
    {
        int srsId = e.GetInt("_identify-srs");
        double X = e.GetDouble("_identify-x");
        double Y = e.GetDouble("_identify-y");
        double mapScale = e.GetDouble("_identify-map-scale");
        string currentActiveToolId = e["_current-activetool-id"];

        var sRef = bridge.CreateSpatialReference(srsId);

        #region Filter

        double toleranceX, toleranceY;
        toleranceX = toleranceY = 15.0 * mapScale / (96.0 / 0.0254);
        if (sRef.IsProjective == false)
        {
            toleranceX = toleranceX * ToDeg / WorldRadius * Math.Cos(Y * ToRad);
            toleranceY = toleranceY * ToDeg / WorldRadius;
        }
        var queryShape = new Envelope(
            X - toleranceX, Y - toleranceY,
            X + toleranceX, Y + toleranceY);

        ApiSpatialFilter filter = new ApiSpatialFilter()
        {
            QueryShape = queryShape,
            FilterSpatialReference = sRef,
            FeatureSpatialReference = sRef,
            QueryGeometry = true,
            Fields = QueryFields.Id
        };

        #endregion

        string queryTheme = e.MenuItemValue;
        var query = await bridge.GetQueryFromThemeId(queryTheme);
        if (query == null)
        {
            throw new Exception("Internal Error: Unkonwn query theme");
        }

        //var shape = await query.FirstFeatureGeometryAsync(filter);

        var featureCollection = await query.PerformAsync(bridge.RequestContext, filter);
        featureCollection.OrderByPointDistance(new Point(X, Y));

        var response = new ApiEventResponse()
        {
            CloseSketch = true,
        };

        if (featureCollection.Count == 1)
        {

            var shape = featureCollection.Where(f => f.Shape != null)
                                         .Select(f => f.Shape)
                                         .FirstOrDefault();

            if (shape != null)
            {
                shape.SrsId = srsId;
                shape.SrsP4Parameters = sRef.Proj4;
            }

            response.Sketch = shape;
        }
        else
        {
            response.NamedSketches = featureCollection.Select(f =>
            {
                var shape = f.Shape;
                if (shape != null)
                {
                    shape.SrsId = srsId;
                    shape.SrsP4Parameters = sRef.Proj4;
                }

                return new WebMapping.Core.Api.EventResponse.Models.NamedSketch()
                {
                    Name = $"ID: {f.Oid}",
                    SubText = localizer.Localize("use-geometry"),
                    Sketch = shape,
                    ZoomOnPreview = false
                };
            });
        }

        if (!String.IsNullOrEmpty(currentActiveToolId))
        {
            var activeTool = bridge.GetTool(currentActiveToolId);
            if (activeTool is IApiPostRequestEvent)
            {
                response = await ((IApiPostRequestEvent)activeTool).PostProcessEventResponseAsync(bridge, e, response);
            }
        }

        return response;
    }

    [ServerToolCommand("pick_attribute")]
    async public Task<ApiEventResponse> OnPickAttribute(IBridge bridge, ApiToolEventArguments e)
    {
        string[] commandArguments = e.ServerCommandArgument?.Split(',')
              .ThrowIfNullOrCountNotEqual(4, () => "Invalid command arguments")
              .ToArray();

        var query = (await bridge.GetQuery(commandArguments[0], commandArguments[1]))
                          .ThrowIfNull(() => $"Unkown query {commandArguments[1]} in service {commandArguments[0]}");
        var filter = new ApiSpatialFilter()
        {
            QueryGeometry = true,
            Fields = QueryFields.All,
            QueryShape = new Envelope(e.MapBBox()),
            FilterSpatialReference = bridge.CreateSpatialReference(Epsg.WGS84),
            FeatureSpatialReference = bridge.CreateSpatialReference(e.CalcCrs.Value)
        };

        var featureCollection = await bridge.QueryLayerAsync(query.GetServiceId(), query.GetLayerId(), filter);

        featureCollection.Project(4326, e.CalcCrs.Value);

        return new ApiEventResponse()
            .AddNamedSketches(
                featureCollection
                    .Where(f => f.Shape != null)
                    .OrderBy(f => f.Shape.ShapeEnvelope.CenterPoint.Distance2D(filter.QueryShape.ShapeEnvelope.CenterPoint))
                    .Select(f =>
                        new WebMapping.Core.Api.EventResponse.Models.NamedSketch()
                            .DoZoomToPreview(false)
                            .DoSetSketch(false)
                            .AddSetter(new UISetter(commandArguments[3], f[commandArguments[2]]))
                            .WithShape(f.Shape)
                            .WithName($"{query.Name} - {f[commandArguments[2]]}")));
    }

    [ServerToolCommand("query_has_attachments")]
    async public Task<ApiEventResponse> OnQueryAttachments(IBridge bridge, ApiToolEventArguments e, ILocalizer<IdentifyDefault> localizer)
    {
        var oids = e.GetArray<string>("feature-oids");
        var oidParts = oids.Select(o => o.Split(":")).ToArray();
        var serviceIds = oidParts.Select(o => o[0]).Distinct().ToArray();
        var queryIds = oidParts.Select(o => o.Length >= 1 ?  o[1] : "").Distinct().ToArray();
        var objectIds = oidParts.Select(o => o.Length >= 2 ? o[2]: "").Distinct().ToArray();

        if(serviceIds.Length != 1 || queryIds.Length != 1)
        {
            throw new Exception("Can't query attachments for multiple service queries...");
        }

        var query = (await bridge.GetQuery(serviceIds[0], queryIds[0])).ThrowIfNull(() => $"Unkown query {queryIds[0]} in service {serviceIds[0]}");

        var ids = (await bridge.GetHasFeatureAttachments(serviceIds[0], query.GetLayerId(), objectIds)).ToImmutableArray();
        var response = new ApiEventResponse();

        foreach (var objectId in objectIds)
        {
            response.AddUISetter(new UICssSetter(
                ids.Contains(objectId)
                    ? UICssSetter.SetterType.AddClass
                    : UICssSetter.SetterType.RemoveClass,
                $"attachments-{serviceIds[0].Replace("@", "-")}-{queryIds[0]}-{objectId}",
                "has-attachments"
                )
            );
            response.AddUISetter(new UICssSetter(
                UICssSetter.SetterType.RemoveClass,
                $"attachments-{serviceIds[0].Replace("@", "-")}-{queryIds[0]}-{objectId}",
                "query-has-attachments"
                )
            );
        }

        return response;
    }

    [ServerToolCommand("show_attachments")]
    async public Task<ApiEventResponse> OnShowAttachments(IBridge bridge, ApiToolEventArguments e, ILocalizer<IdentifyDefault> localizer)
    {
        string[] oidParts = e["feature-oid"]?.Split(":");
        if (oidParts?.Length != 3)
        {
            throw new ArgumentException($"Invalid OID format. Expected format: <serviceId>:<layerId>:<oid> not {e["feature-oid"]?.Split(":")}");
        }

        var query = (await bridge.GetQuery(oidParts[0], oidParts[1])).ThrowIfNull(() => $"Unknown query {oidParts[1]} in service {oidParts[0]}");
        var attachments = await bridge.GetFeatureAttachments(oidParts[0], query.GetLayerId(), oidParts[2]);

        if (attachments?.Attachements?.Any() != true)
        {
            throw new Exception($"No attachments found for this feature (oid={oidParts[2]})");
        }

        var dialog = new UIDiv()
            .AsDialog(UIElementTarget.tool_sidebar_left)
            .WithStyles(UICss.NarrowForm)
            .WithDialogTitle(localizer.Localize("attachments"));

        foreach (var attachment in attachments.Attachements)
        {
            dialog.AddChild(new UILiteralBold() { literal = attachment.Name });
            dialog.AddChild(new UIBreak());
            dialog.AddChild(new UIImage(
                Convert.ToBase64String(attachment.Data),
                true));
        }

        return new ApiEventResponse().
            AddUIElements(dialog);     
    }

    #endregion

    #region Classes



    #endregion
}
