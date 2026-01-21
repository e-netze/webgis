using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.DTOs.Events;
using E.Standard.Api.App.DTOs.Tools;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.DependencyInjection;
using E.Standard.DependencyInjection.Abstractions;
using E.Standard.Json;
using E.Standard.Localization.Extensions;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestToolsHelperService
{
    private readonly ILogger<RestToolsHelperService> _logger;
    private readonly RestHelperService _restHelper;
    private readonly CacheService _cache;
    private readonly ConfigurationService _config;
    private readonly ICryptoService _crypto;
    private readonly IUrlHelperService _urlHelper;
    private readonly IStringLocalizer _stringLocalizer;

    public RestToolsHelperService(ILogger<RestToolsHelperService> logger,
                                  RestHelperService restHelper,
                                  CacheService cache,
                                  ConfigurationService config,
                                  ICryptoService crypto,
                                  UrlHelperService urlHelper,
                                  IStringLocalizerFactory localizerFactory)
    {
        _logger = logger;
        _restHelper = restHelper;
        _cache = cache;
        _config = config;
        _crypto = crypto;
        _urlHelper = urlHelper;
        _stringLocalizer = localizerFactory.Create(typeof(RestToolsHelperService));
    }

    public ToolDTO Create(IApiButton apiTool)
    {
        ToolDTO tool = null;

        if (apiTool is IApiClientButton)
        {
            tool = new ClientButtonToolDTO()
            {
                command = ((IApiClientButton)apiTool).ClientCommand.ToString().ToLower()
            };
        }
        else if (apiTool.GetType().IsApiServerButton())
        {
            tool = new ServerButtonToolDTO();
        }
        else if (apiTool.GetType().IsApiServerTool())
        {
            tool = new ServerToolDTO()
            {
                tooltype = ((IApiTool)apiTool).Type.ToString().ToLower()
            };
        }
        else if (apiTool.GetType().IsApiClientTool())
        {
            tool = new ClientToolDTO()
            {
                tooltype = ((IApiTool)apiTool).Type.ToString().ToLower()
            };
        }
        else
        {
            tool = new ToolDTO();
        }

        if (apiTool is IGraphicsTool)
        {
            tool.IsGraphicsTool = true;
        }

        if (apiTool is IAdvancedSketchTool)
        {
            tool.SketchOnlyEditableIfToolTabIsActive = ((IAdvancedSketchTool)apiTool).SketchOnlyEditableIfToolTabIsActive;
        }

        tool.ClientName = apiTool.GetType().GetCustomAttribute<ToolClientAttribute>()?.ClientName?.ToLower();
        tool.id = apiTool.GetType().ToToolId();
        tool.container = apiTool.LocalizedContainer(_stringLocalizer);
        tool.name = apiTool.LocalizedName(_stringLocalizer);
        tool.image = apiTool.Image;
        tool.tooltip = apiTool.LocalizedToolTip(_stringLocalizer);
        tool.hasui = apiTool.HasUI;

        if (apiTool is IApiTool)
        {
            var cursor = ((IApiTool)apiTool).Cursor.ToString().ToLower();
            if (cursor.StartsWith("custom_"))
            {
                tool.cursor = cursor.Substring("custom_".Length) + ".cur";
            }
            else
            {
                tool.cursor = cursor;
            }
        }

        if (apiTool is IApiChildTool)
        {
            tool.is_childtool = true;
            if (((IApiChildTool)apiTool).ParentTool != null)
            {
                tool.parentid = ((IApiChildTool)apiTool).ParentTool.GetType().ToToolId();
            }
        }
        if (apiTool is IApiButtonDependency)
        {
            List<string> buttonDependencies = new List<string>();
            foreach (VisibilityDependency dependency in Enum.GetValues(typeof(VisibilityDependency)))
            {
                if (((int)((IApiButtonDependency)apiTool).ButtonDependencies & (int)dependency) == (int)dependency)
                {
                    buttonDependencies.Add(dependency.ToString().ToLower());
                }
            }
            tool.dependencies = buttonDependencies.ToArray();
        }

        if(apiTool is IApiToolSketchProperties toolSketchProperties)
        {
            var e = CreateApiToolEventArguments(apiTool, "", null);
            tool.MaxSketchVertices = toolSketchProperties.MaxToolSketchVertices(e);
        }

        if (apiTool is IApiToolConfirmation)
        {
            List<ToolConfirmMessageDTO> confirmations = new List<ToolConfirmMessageDTO>();
            if (((IApiToolConfirmation)apiTool).ToolConfirmations != null)
            {
                foreach (var confirmation in ((IApiToolConfirmation)apiTool).ToolConfirmations)
                {
                    confirmations.Add(new ToolConfirmMessageDTO()
                    {
                        command = confirmation.Command,
                        message = apiTool.LocalizeButtonProperty(_stringLocalizer, confirmation.Message, () => confirmation.Message),
                        type = confirmation.Type.ToString().ToLower(),
                        eventtype = confirmation.EventType.ToString().ToLower()
                    });
                }
            }
            if (confirmations.Count > 0)
            {
                tool.confirmmessages = confirmations.ToArray();
            }
        }

        if (apiTool is IApiToolMarker && ((IApiToolMarker)apiTool).Marker != null)
        {
            tool.marker = ((IApiToolMarker)apiTool).Marker.ToJsonMarker();
        }

        if (apiTool is IApiToolPersistenceContext && ((IApiToolPersistenceContext)apiTool).PersistenceContextTool != null)
        {
            tool.persistencecontext = ((IApiToolPersistenceContext)apiTool).PersistenceContextTool.ToToolId().ToString().ToLower();
        }

        List<string> handlers = new List<string>();
        foreach (var method in apiTool.GetType().GetMethods())
        {
            foreach (ServerEventHandlerAttribute serverEventHandler in method.GetCustomAttributes(typeof(ServerEventHandlerAttribute), true) ?? new ServerEventHandlerAttribute[0])
            {
                if (!handlers.Contains(serverEventHandler.Handler.ToString().ToLower()))
                {
                    handlers.Add(serverEventHandler.Handler.ToString().ToLower());
                }
            }
        }
        if (handlers.Count > 0)
        {
            tool.EventHandlers = handlers.ToArray();
        }

        var advancedProperties = apiTool.GetType().GetCustomAttribute<AdvancedToolPropertiesAttribute>();
        if (advancedProperties != null)
        {
            tool.VisFilterDependent = advancedProperties.VisFilterDependent ? true : null;
            tool.LabelingDependent = advancedProperties.LabelingDependent ? true : null;
            tool.AllowCtrlBBox = advancedProperties.AllowCtrlBBox ? true : null;
            tool.SelectionInfoDependent = advancedProperties.SelectionInfoDependent ? true : null;
            tool.ScaleDependent = advancedProperties.ScaleDependent ? true : null;
            tool.MapCrsDependent = advancedProperties.MapCrsDependent ? true : null;
            tool.MapBBoxDependent = advancedProperties.MapBBoxDependent ? true : null;
            tool.PrintLayoutRotationDependent = advancedProperties.PrintLayoutRotationDependent ? true : null;
            tool.MapImageSizeDependent = advancedProperties.MapImageSizeDependent ? true : null;
            tool.AnonymousUserIdDependent = advancedProperties.AnonymousUserIdDependent ? true : null;
            tool.ClientDeviceDependent = advancedProperties.ClientDeviceDependent ? true : null;
            tool.AsideDialogExistsDependent = advancedProperties.AsideDialogExistsDependent ? true : null;
            tool.LiveShareClientnameDependent = advancedProperties.LiveShareClientnameDependent ? true : null;
            tool.CustomServiceRequestParametersDependent = advancedProperties.CustomServiceRequestParametersDependent ? true : null;
            tool.StaticOverlayServicesDependent = advancedProperties.StaticOverlayServicesDependent ? true : null;
            tool.UIElementDependent = advancedProperties.UIElementDependency ? true : null;
            tool.QueryMarkersVisibilityDependent = advancedProperties.QueryMarkersVisibliityDependent ? true : null;
            tool.CoordinateMarkersVisibilityDependent = advancedProperties.CoordinateMarkersVisibilityDependent ? true : null;
            tool.ChainageMarkersVisibilityDependent = advancedProperties.ChainageMarkersVisibilityDependent ? true : null;
            tool.UIElementFocus = advancedProperties.UIElementFocus != E.Standard.WebGIS.Core.FocusableUIElements.None ?
                                                advancedProperties.UIElementFocus.ToString().Split(',').Select(s => s.Trim()).ToArray() :
                                                null;
        }

        var toolHelpAttribute = apiTool.GetType().GetCustomAttribute<ToolHelpAttribute>();
        if (toolHelpAttribute != null)
        {
            tool.HelpUrlPath = toolHelpAttribute.UrlPath;
            tool.HelpUrlPathDefaultTool = toolHelpAttribute.UrlPathDefaultTool;
        }

        return tool;
    }


    async public Task<T> InvokeServerCommandAsync<T, TToolType>(string command, Bridge bridge, ApiToolEventArguments e)
    {
        var tool = Activator.CreateInstance<TToolType>();

        var methodInfo = tool.GetType().GetMethods()
                                .Where(m =>
                                {
                                    var toolCommandAttribte = m.GetCustomAttribute<ServerToolCommandAttribute>();
                                    return (toolCommandAttribte != null && toolCommandAttribte.Method.Equals(command));
                                })
                                .FirstOrDefault();

        if (methodInfo != null)
        {
            var currentArguments = bridge.CurrentEventArguments;
            bridge.CurrentEventArguments = e;

            var dependencyProvider = new ToolDependencyProvider(bridge, e, _stringLocalizer);

            var result = await InvokeMethodAsync<T>(methodInfo, tool, dependencyProvider);

            bridge.CurrentEventArguments = currentArguments;

            return result;
        }

        return default(T);
    }

    async public Task<T> InvokeMethodAsync<T>(System.Reflection.MethodInfo methodInfo, object instance, IDependencyProvider dependencyProvider)
    {
        try
        {
            var response = methodInfo.Invoke(instance, Invoker.GetDependencies(methodInfo, dependencyProvider).ToArray());

            if (response is Task<T>)
            {
                return await (Task<T>)response;
            }

            return (T)response;
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException;
        }
    }

    public ApiToolEventArguments CreateApiToolEventArguments(IApiButton tool, string eventString, string toolOptions)
    {
        return new ApiToolEventArguments(eventString, toolOptions, configuration: tool.ToolConfiguration(_config));
    }

    async public Task<IActionResult> ToolResponseResult(ApiBaseController controller, ApiEventResponse apiResponse, CmsDocument.UserIdentification ui)
    {
        ToolEventResponseDTO response = new ToolEventResponseDTO();

        if (apiResponse is ApiRawJsonEventResponse rawJsonResponse)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(JSerializer.Serialize(rawJsonResponse.RawJsonObject));
            }

            return await controller.JsonObject(rawJsonResponse.RawJsonObject);
        }
        if (apiResponse is ApiRawStringEventResponse rawStringResponse)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(rawStringResponse.RawString);
            }

            return controller.PlainView(rawStringResponse.RawString, ((ApiRawStringEventResponse)apiResponse).ContentType);
        }
        if (apiResponse is ApiRawDownloadEventResponse)
        {
            var downloadResponse = (ApiRawDownloadEventResponse)apiResponse;

            string fileName = $"tmp{Guid.NewGuid().ToString("N").ToLower()}.dat";

            await downloadResponse.RawBytes.SaveOrUpload($"{_urlHelper.OutputPath()}/{fileName}");

            return await controller.JsonObject(new
            {
                action = "download",
                downloadId = _crypto.EncryptTextDefault(fileName, CryptoResultStringType.Hex),
                name = downloadResponse.Name,
                contentType = downloadResponse.ContentType,
                successs = true
            });
        }
        if (apiResponse is ApiRawBytesEventResponse)
        {
            return controller.RawResponse(
                ((ApiRawBytesEventResponse)apiResponse).RawBytes,
                ((ApiRawBytesEventResponse)apiResponse).ContentType,
                ((ApiRawBytesEventResponse)apiResponse).Headers);
        }

        if (apiResponse is ApiFeaturesEventResponse)
        {
            var featuresResponse = (ApiFeaturesEventResponse)apiResponse;

            if (featuresResponse.Features != null)
            {
                #region Determine Meta Tool Type

                var queryTool = FeaturesDTO.Meta.Tool.Unknown;
                if (featuresResponse.Filter != null)
                {
                    if (!String.IsNullOrWhiteSpace(featuresResponse.Filter.Tool) && Enum.TryParse(featuresResponse.Filter.Tool, true, out queryTool))
                    {
                        // use from query
                    }
                    else if (featuresResponse.Filter is ApiSpatialFilter)
                    {
                        var shape = ((ApiSpatialFilter)featuresResponse.Filter).QueryShape;
                        if (shape is Point)
                        {
                            queryTool = FeaturesDTO.Meta.Tool.PointIdentify;
                        }
                        else if (shape is Polyline)
                        {
                            queryTool = FeaturesDTO.Meta.Tool.LineIdentify;
                        }
                        else if (shape is Polygon)
                        {
                            queryTool = FeaturesDTO.Meta.Tool.PolygonIdentify;
                        }
                        else
                        {
                            queryTool = FeaturesDTO.Meta.Tool.Identify;
                        }
                    }
                    else
                    {
                        queryTool = FeaturesDTO.Meta.Tool.Query;
                    }
                }

                #endregion

                response = new FeaturesResponseDTO()
                {
                    zoomtoresults = ((ApiFeaturesEventResponse)apiResponse).ZoomToResults,
                    querytoolid = ((ApiFeaturesEventResponse)apiResponse).QueryToolId
                };

                if (featuresResponse.Query is QueryDTO && featuresResponse.Query.Distinct == true)
                {
                    featuresResponse.Features.Distinct(true, ((QueryDTO)featuresResponse.Query).AllFieldNames());
                }

                var resultFeatureCollection = await _restHelper.PrepareFeatureCollection(
                            featuresResponse.Features,
                            featuresResponse.Query as QueryDTO,
                            featuresResponse.FeatureSpatialReference,
                            ui,
                            featuresResponse.ClickEvent,
                            appendHoverShape: featuresResponse.AppendHoverShapes);

                // Bei AddToSelection müssen die Links für alle Features neu berechnet werden
                //if (featuresResponse.FeaturesForLinks != null)
                //{
                //    resultFeatureCollection.Append1toNLinks(featuresResponse.FeaturesForLinks,
                //                                            featuresResponse.Query as Query);
                //}

                var resultFeatures = new FeaturesDTO(
                        resultFeatureCollection,
                        queryTool,
                        ((ApiFeaturesEventResponse)apiResponse).SelectResults,
                        method: ((ApiFeaturesEventResponse)apiResponse).FeatureResponseType,
                        sortColumns: (featuresResponse.Query as QueryDTO)?.AutoSortFields,
                        customSelection: ((ApiFeaturesEventResponse)apiResponse).CustomSelectionId);

                //resultFeatures.features = null;

                #region Check if visfilters can applied

                var query = ((ApiFeaturesEventResponse)apiResponse).Query as QueryDTO;
                if (resultFeatures.metadata != null && query?.Service?.Layers != null)
                {
                    var layer = query.Service.Layers.FindByLayerId(query.LayerId);
                    if (layer != null)
                    {
                        List<VisFilterDTO> applicableVisFilters = new List<VisFilterDTO>();
                        foreach (var visFilter in _cache.GetAllVisFilters(query.Service.Url, ui).filters.Where(f => f.FilterType != E.Standard.WebGIS.CMS.VisFilterType.locked))
                        {
                            if ((visFilter.LayerNamesString ?? String.Empty).Split(';').Contains(layer.Name))
                            {
                                var firstFeature = ((ApiFeaturesEventResponse)apiResponse).Features.FirstOrDefault();
                                if (firstFeature != null)
                                {
                                    bool hasAllParameters = visFilter.Parameters.Keys.Count > 0;
                                    foreach (var parameter in visFilter.Parameters.Keys)
                                    {
                                        if (firstFeature.Attributes.Where(a => a.Name == parameter).Count() == 0)
                                        {
                                            hasAllParameters = false;
                                            break;
                                        }
                                    }

                                    if (hasAllParameters)
                                    {
                                        applicableVisFilters.Add(visFilter);
                                    }
                                }
                            }
                        }

                        if (applicableVisFilters.Count > 0)
                        {
                            resultFeatures.metadata.ApplicableVisFilters =
                                String.Join(",", applicableVisFilters.Select(f => f.Id.ToUniqueFilterId(query.Service.Url)).ToArray());
                        }
                    }
                }

                #endregion

                if (featuresResponse.Query?.Union == true)
                {
                    resultFeatures.Union();
                }

                ((FeaturesResponseDTO)response).features = resultFeatures;
            }
            if (featuresResponse.FeaturesForLinks != null)  // nur 1:n Links neu setzen (zB bei remove from selection)
            {
                var linkFeatures = new E.Standard.WebMapping.Core.Collections.FeatureCollection();
                linkFeatures.Append1toNLinks(featuresResponse.FeaturesForLinks,
                                             featuresResponse.Query as QueryDTO,
                                             requestHeaders: controller?.Request?.HeadersCollection());

                response.FeatureCollectionLinks = linkFeatures.Links;
            }
        }

        if (apiResponse is ApiDynamicContentEventResponse)
        {
            response.DynamicContent = new DynamicContentEventDTO()
            {
                Id = "dyncontent_" + Guid.NewGuid().ToString("N").ToLower(),
                Name = ((ApiDynamicContentEventResponse)apiResponse).Name,
                Url = ((ApiDynamicContentEventResponse)apiResponse).Url,
                Type = ((ApiDynamicContentEventResponse)apiResponse).Type.ToString().ToLower(),
            };
        }

        if (apiResponse is ApiPrintEventResponse)
        {
            if (!((ApiPrintEventResponse)apiResponse).Path.ToLower().Replace("\\", "/").StartsWith(_urlHelper.OutputPath().ToLower().Replace("\\", "/")))
            {
                throw new ArgumentException("Print result in forbidden directory!");
            }
            var outputRelFileName = ((ApiPrintEventResponse)apiResponse).Path.Substring(_urlHelper.OutputPath().Length).Replace("\\", "/");
            while (outputRelFileName.StartsWith("/"))
            {
                outputRelFileName = outputRelFileName.Substring(1);
            }

            response.PrintContent = new PrintContentDTO()
            {
                Url = ((ApiPrintEventResponse)apiResponse).Url,
                preview = ((ApiPrintEventResponse)apiResponse).PreviewUrl,
                Length = ((ApiPrintEventResponse)apiResponse).Length,
                DownloadId = _crypto.EncryptTextDefault(outputRelFileName, CryptoResultStringType.Hex)
            };
        }

        if (apiResponse.UIElements != null || apiResponse.UISetters != null)
        {
            response.ui = new ToolUiDTO();
            response.ui.elements = apiResponse.UIElements?.ToArray();
            response.ui.setters = apiResponse.UISetters?.ToArray();
            if (apiResponse.AppendUIElements.HasValue)
            {
                response.ui.append = apiResponse.AppendUIElements.Value;
            }
        }

        if (apiResponse.Events != null)
        {
            List<ApiToolEventDTO> toolEvents = new List<ApiToolEventDTO>();
            foreach (IApiToolEvent @event in apiResponse.Events)
            {
                toolEvents.Add(new ApiToolEventDTO(@event));
            }
            response.toolevents = toolEvents.ToArray();
        }

        if (apiResponse.ActiveTool != null)
        {
            response.activetool = Create(apiResponse.ActiveTool);
        }
        if (apiResponse.ActiveToolType != null)
        {
            response.setactivetooltype = apiResponse.ActiveToolType.ToString().ToLower();
        }
        if (apiResponse.ToolCursor.HasValue)
        {
            var cursor = apiResponse.ToolCursor.ToString().ToLower();
            if (cursor.StartsWith("custom_"))
            {
                response.ToolCursor = "custom";
                response.ToolCursor = cursor.Substring("custom_".Length) + ".cur";
            }
            else
            {
                response.ToolCursor = cursor;
            }
        }
        if (apiResponse.Graphics != null)
        {
            response.graphics = new ApiGraphicsResponseDTO();
            if (apiResponse.Graphics.ActiveGraphicsTool != GraphicsTool.None)
            {
                response.graphics.activegraphicstool = apiResponse.Graphics.ActiveGraphicsTool.ToString().ToLower();
            }
            response.graphics.geojson = apiResponse.Graphics.Elements;
            response.graphics.replaceelements = apiResponse.Graphics.ReplaceElements;
            response.graphics.suppressZoom = apiResponse.Graphics.SuppressZoom == true ? true : null;
            response.graphics.symbols = apiResponse.Graphics.SymbolsDefinition;
        }
        if (apiResponse.MapViewLense != null)
        {
            response.MapViewLense = new LenseEventDTO()
            {
                Width = apiResponse.MapViewLense.Width,
                Height = apiResponse.MapViewLense.Height,
                Options = new LenseEventDTO.LenseOptions()
                {
                    Zoom = apiResponse.MapViewLense.Zoom,
                    ScaleControlId = apiResponse.MapViewLense.ScaleControl,
                    LenseScale = apiResponse.MapViewLense.LenseScale
                }
            };
        }
        if (apiResponse.SketchVertexTooltips != null)
        {
            List<SketchVertexToolTipEventDTO> tooltips = new List<SketchVertexToolTipEventDTO>();
            foreach (var sketchVertexTooltip in apiResponse.SketchVertexTooltips)
            {
                tooltips.Add(new SketchVertexToolTipEventDTO()
                {
                    Latitude = sketchVertexTooltip.Latitude,
                    Longitude = sketchVertexTooltip.Longitude,
                    Tooltip = sketchVertexTooltip.ToolTip
                });
            }
            if (tooltips.Count > 0)
            {
                response.SketchVertexTooltips = tooltips.ToArray();
            }
        }
        if (apiResponse.SketchAddVertex != null)
        {
            response.SketchAddVertex = new double[] { apiResponse.SketchAddVertex.X, apiResponse.SketchAddVertex.Y };
        }
        if (apiResponse.SketchProperties is not null)
        {
            response.SketchProperties = apiResponse.SketchProperties;
        }
        if (apiResponse.SetFilters != null && apiResponse.SetFilters.Length > 0)
        {
            response.SetFilters = apiResponse.SetFilters;
        }
        if (apiResponse.UnsetFilters != null && apiResponse.UnsetFilters.Length > 0)
        {
            response.UnsetFilters = apiResponse.UnsetFilters;
        }
        if (apiResponse.SetLabeling != null && apiResponse.SetLabeling.Length > 0)
        {
            response.SetLabeling = apiResponse.SetLabeling;
        }
        if (apiResponse.UnsetLabeling != null && apiResponse.UnsetLabeling.Length > 0)
        {
            response.UnsetLabeling = apiResponse.UnsetLabeling;
        }
        if (apiResponse.AddStaticOverlayServices != null && apiResponse.AddStaticOverlayServices.Length > 0)
        {
            response.AddStaticOverlayServices = apiResponse.AddStaticOverlayServices;
        }
        if (apiResponse.RemoveStaticOverlayServices != null && apiResponse.RemoveStaticOverlayServices.Length > 0)
        {
            response.RemoveStaticOverlayServices = apiResponse.RemoveStaticOverlayServices;
        }
        if (apiResponse is MapJsonResponse)
        {
            var mapJsonResponse = (MapJsonResponse)apiResponse;
            if (!String.IsNullOrWhiteSpace(mapJsonResponse.SerializationMapJson))
            {
                response.serializationmapjson = mapJsonResponse.SerializationMapJson;
            }
            if (!String.IsNullOrWhiteSpace(mapJsonResponse.Master0Json))
            {
                response.ui_master0 = mapJsonResponse.Master0Json;
            }
            if (!String.IsNullOrWhiteSpace(mapJsonResponse.Master1Json))
            {
                response.ui_master1 = mapJsonResponse.Master1Json;
            }
            if (!String.IsNullOrWhiteSpace(mapJsonResponse.MapDescription))
            {
                response.map_description = mapJsonResponse.MapDescription;
            }
            if (!String.IsNullOrWhiteSpace(mapJsonResponse.MapTitle))
            {
                response.map_title = mapJsonResponse.MapTitle;
            }
        }
        if (apiResponse is ThreeDResponse)
        {
            response.ThreeD_BoundingBox = ((ThreeDResponse)apiResponse).BoundingBox;
            response.ThreeD_BoundingBox_Epsg = ((ThreeDResponse)apiResponse).BoundBoxEpsg;
            response.ThreeD_ArraySize = ((ThreeDResponse)apiResponse).ArraySize;
            response.ThreeD_Values = ((ThreeDResponse)apiResponse).Values;
            response.ThreeD_Texture = ((ThreeDResponse)apiResponse).Texture.ToString().ToLower();
            response.ThreeD_TextureOrhtoService = ((ThreeDResponse)apiResponse).TextureOrthoService;
            response.ThreeD_TextureStreetsOverlayService = ((ThreeDResponse)apiResponse).TextureStreetsOverlayService;
        }
        if (apiResponse.Sketch != null)
        {
            response.sketch = E.Standard.Api.App.DTOs.Geometry.GeometryDTO.FromShape(apiResponse.Sketch);
            if (apiResponse.CloseSketch == true)
            {
                response.close_sketch = true;
            }

            if (apiResponse.FocusSketch == true)
            {
                response.FocusSketch = true;
            }

            if (apiResponse.SketchReadonly == true)
            {
                response.sketch_readonly = true;
            }
        }
        if (apiResponse.NamedSketches != null && apiResponse.NamedSketches.Count() > 0)
        {
            if (apiResponse.CloseSketch == true)
            {
                response.close_sketch = true;
            }

            response.named_sketches = apiResponse.NamedSketches.Select(n => new NamedSketchDTO()
            {
                Name = n.Name,
                SubText = n.SubText,
                Sketch = E.Standard.Api.App.DTOs.Geometry.GeometryDTO.FromShape(n.Sketch),
                ZoomOnPreview = n.ZoomOnPreview,
                SetSketch = n.SetSketch,
                UISetters = n.UISetters?.ToArray()
            });
        }
        if (apiResponse.RemoveSketch == true)
        {
            response.remove_sketch = true;
        }
        if (apiResponse.SketchHasZ.HasValue)
        {
            response.sketch_hasZ = apiResponse.SketchHasZ.Value;
        }
        if (!String.IsNullOrEmpty(apiResponse.SketchGetZCommand))
        {
            response.sketch_getZ_command = apiResponse.SketchGetZCommand;
        }

        if (!String.IsNullOrWhiteSpace(apiResponse.InitLiveshareConnection))
        {
            response.init_liveshare_connection = apiResponse.InitLiveshareConnection;
        }
        if (!String.IsNullOrWhiteSpace(apiResponse.JoinLiveshareSession))
        {
            response.join_liveshare_session = apiResponse.JoinLiveshareSession;
        }
        if (!String.IsNullOrWhiteSpace(apiResponse.LeaveLiveshareSession))
        {
            response.leave_liveshare_session = apiResponse.LeaveLiveshareSession;
        }
        if (apiResponse.ExitLiveShare == true)
        {
            response.exit_liveshare = true;
        }
        if (!String.IsNullOrWhiteSpace(apiResponse.SetLiveShareClientname))
        {
            response.set_liveshare_clientname = apiResponse.SetLiveShareClientname;
        }

        if (apiResponse.ZoomTo4326 != null && apiResponse.ZoomTo4326.Length == 4)
        {
            response.zoom_to_4326 = apiResponse.ZoomTo4326;
            response.zoom_to_scale = apiResponse.ZoomToScale;
        }

        if (apiResponse.Chart != null)
        {
            response.chart = new E.Standard.Api.App.DTOs.Drawing.ChartDTO(apiResponse.Chart);
        }
        if (apiResponse.RefreshServices != null)
        {
            response.refreshservices = apiResponse.RefreshServices;
        }
        if (apiResponse.SetLayerVisility != null)
        {
            response.setlayervisibility = apiResponse.SetLayerVisility;
        }
        if (apiResponse.ToolSelection != null)
        {
            response.toolselection = apiResponse.ToolSelection;
        }
        if (apiResponse.RefreshSelection)
        {
            response.refreshselection = true;
        }
        if (apiResponse.RefreshSnapping)
        {
            response.RefreshSnapping = true;
        }
        if (!String.IsNullOrEmpty(apiResponse.TriggerToolButtonClick))
        {
            response.triggerToolButtonClick = apiResponse.TriggerToolButtonClick;
        }
        if (apiResponse.ClientCommands != null)
        {
            response.ClientCommands = apiResponse.ClientCommands.Select(c => c.ToString()).ToArray();
            response.ClientCommandData = apiResponse.ClientCommandData;
        }
        if (apiResponse.ToolUndos != null)
        {
            foreach (var undos in apiResponse.ToolUndos)
            {
                if (undos.PreviewShape != null)
                {
                    undos.Preview = E.Standard.Api.App.DTOs.Geometry.GeometryDTO.FromShape(undos.PreviewShape);
                }
            }
            response.ToolUndos = apiResponse.ToolUndos;
            if (apiResponse.UndoTool != null)
            {
                response.UndoToolId = apiResponse.UndoTool.GetType().ToToolId();
            }
        }
        if (apiResponse.ApplyEditingTheme != null)
        {
            response.ApplyEditingTheme = apiResponse.ApplyEditingTheme;
        }
        if (apiResponse.ReplaceQueryFeatures != null)
        {
            FeaturesDTO replaceQueryFeatures = null;

            foreach (var replaceFeatureQuery in apiResponse.ReplaceFeaturesQueries)
            {
                var features = new FeaturesDTO(
                        await _restHelper.PrepareFeatureCollection(
                                apiResponse.ReplaceQueryFeatures,
                                replaceFeatureQuery as QueryDTO,
                                apiResponse.ReplaceFeatureSpatialReference,
                                ui
                            ));
                if (replaceQueryFeatures == null)
                {
                    replaceQueryFeatures = features;
                }
                else
                {
                    replaceQueryFeatures.AppendFeatures(features);
                }
            }

            response.ReplaceQueryFeatures = replaceQueryFeatures;
        }
        if (apiResponse.RemoveQueryFeaturesById != null)
        {
            response.RemoveQueryFeaturesById = _restHelper.PrepareFeatureGlobalsOids(apiResponse.RemoveQueryFeaturesById,
                                                                                     apiResponse.RemoveFeaturesQueries.Select(q => q as QueryDTO)).ToArray();
        }
        if (!String.IsNullOrEmpty(apiResponse.ErrorMessage))
        {
            response.ErrorMessage = apiResponse.ErrorMessage;
        }
        if (!String.IsNullOrEmpty(apiResponse.FireCustomMapEvent))
        {
            response.FireCustomMapEvent = apiResponse.FireCustomMapEvent;
        }
        if (apiResponse.RemoveSecondaryToolUI)
        {
            response.RemoveSecondaryToolUI = apiResponse.RemoveSecondaryToolUI;
        }

        return await controller.JsonObject(response);
    }
}
