using E.Standard.Converters.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.DynamicLayers;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Renderers;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.RequestBuilders;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using E.Standard.WebMapping.GeoServices.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

public class MapService : IMapService2,
                          IMapServiceFuzzyLayerNames,
                          IServiceLegend,
                          IServiceLegend2,
                          IServiceProjection,
                          IServiceDatumTransformations,
                          IServiceSelectionProperties,
                          IExportableOgcService,
                          IServiceSupportedCrs,
                          IServiceCopyrightInfo,
                          IFeatureWorkspaceProvider,
                          IServiceDescription,
                          IServiceInitialException,
                          IDynamicService,
                          IImageServiceType,
                          IMapServiceAuthentication
{
    private readonly LayerCollection _layers;
    private string _errMsg = "", _mapServiceName = String.Empty;
    private ErrorResponse _initErrorResponse = null, _diagnosticsErrorResponse = null;
    private string _imageFormat = "png";
    private ServiceTheme[] _serviceThemes = null;

    private int _maxImageWidth = 0, _maxImageHeight = 0;

    public MapService()
    {
        _layers = new LayerCollection(this);
        this.UseToc = this.ShowInToc = true;
        this.TokenExpiration = 3600;
    }

    #region IService Members

    public string Name
    {
        get;
        set;
    }

    public string Url
    {
        get;
        set;
    }

    public string Server
    {
        get;
        private set;
    }

    public string Service
    {
        get;
        private set;
    }

    public string ServiceShortname { get { return _mapServiceName; } }

    public string ID
    {
        get;
        private set;
    }

    public float Opacity
    {
        get;
        set;
    }

    public bool CanBuffer
    {
        get { return false; }
    }

    public bool UseToc
    {
        get;
        set;
    }

    public LayerCollection Layers
    {
        get { return _layers; }
    }

    public Envelope InitialExtent
    {
        get;
        private set;
    }

    public ServiceResponseType ResponseType
    {
        get { return ServiceResponseType.Image; }
    }

    public ServiceDiagnostic Diagnostics { get; private set; }
    public ServiceDiagnosticsWarningLevel DiagnosticsWaringLevel { get; set; }

    public bool PreInit(string serviceID, string server, string url, string authUser, string authPwd, string staticToken, string appConfigPath, ServiceTheme[] serviceThemes)
    {
        this.ID = serviceID;
        this.Server = server;
        this.Service = url;

        this.Username = authUser;
        this.Password = authPwd;
        this.StaticToken = staticToken;

        _serviceThemes = serviceThemes;

        _mapServiceName = MapServiceName(this.Service);

        return true;
    }

    async public Task<bool> InitAsync(IMap map, IRequestContext requestContext)
    {
        this.Map = map;

        _initErrorResponse = null;
        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(map, this.Server, _mapServiceName, "Init", $"Init {this.Server} {this.Service.Replace(" ", "_")}"))
        {
            try
            {
                string jsonStringAnswer = await authHandler.TryPostAsync(
                                this,
                                this.Service,
                                ServiceInfoRequestBuilder.DefaultRequest);

                if (requestContext.Trace)
                {
                    requestContext.GetRequiredService<IGeoServiceRequestLogger>()
                        .LogString(this.Service, _mapServiceName, "initasync-service", jsonStringAnswer);
                }

                JsonService jsonService = JSerializer.Deserialize<JsonService>(jsonStringAnswer);

                this.ServiceDescription = jsonService.ServiceDescription.OrTake(jsonService.Description).ToMarkdownString();
                this.CopyrightText = jsonService.CopyrightText.ToMarkdownString(); ;

                jsonStringAnswer = await authHandler.TryPostAsync(
                                    this,
                                    $"{this.Service}/layers",
                                    ServiceLayersRequestBuilder.DefaultRequest);

                if (requestContext.Trace)
                {
                    requestContext.GetRequiredService<IGeoServiceRequestLogger>()
                        .LogString($"{this.Service}/layers", _mapServiceName, "initasync-layers", jsonStringAnswer);
                }

                JsonLayers jsonLayers = JSerializer.Deserialize<JsonLayers>(jsonStringAnswer); // equiv with map description

                if (String.IsNullOrEmpty(this.Name))
                {
                    this.Name = !String.IsNullOrEmpty(jsonService.MapName) && jsonService.MapName != "Layer" ? jsonService.MapName : this.ServiceShortname;
                }

                if (!jsonLayers.HasDataLayers())
                {
                    throw new Exception("Service has not layers");
                }
                else
                {
                    jsonLayers.SetParentLayers();

                    #region Layers

                    // Temporäre Liste => Falls Init mehrfach/gleichzeitg aufgerufen wird
                    // Am schluss dann an LayerCollection übergeben
                    List<Layer> layers = new List<Layer>();

                    foreach (var jsonLayer in jsonLayers.Layers)
                    {
                        LayerType layerType = LayerType.unknown;
                        switch (jsonLayer.GeometryType)
                        {
                            case "esriGeometryPolyline":
                                layerType = LayerType.line;
                                break;
                            case "esriGeometryPolygon":
                                layerType = LayerType.polygon;
                                break;
                            case "esriGeometryMultipoint":
                            case "esriGeometryPoint":
                                layerType = LayerType.point;
                                break;
                            default:
                                layerType = LayerType.unknown;
                                break;
                        }

                        string layerName;

                        if (jsonLayer != null && jsonLayer.ParentLayer != null && jsonLayer.ParentLayer.Type == "Annotation Layer")
                        {
                            //layerParentPathHelper.Append("(" + jsonLayer.ParentLayer.Name + " " + jsonLayer.Name + ")"); // ...parent0\parent1\parent1 (layername)
                            layerName = $"{jsonLayer.ParentFullName}{jsonLayer.ParentLayer.Name} ({jsonLayer.Name})";
                        }
                        else
                        {
                            //layerParentPathHelper.Append(jsonLayer.Name); // ...parent0\parent1\layername
                            layerName = jsonLayer.FullName;
                        }

                        if (jsonLayer.Type == "Feature Layer" || jsonLayer.Type == "Feature-Layer")
                        {
                            FeatureLayer layer = new FeatureLayer(
                                layerName,
                                jsonLayer.Id.ToString(),
                                layerType,
                                this,
                                queryable: true)
                            {
                                HasM = jsonLayer.HasM,
                                HasZ = jsonLayer.HasZ,
                                Visible = jsonLayer.DefaultVisibility
                            };

                            if (jsonLayer.Fields != null)
                            {
                                foreach (var field in jsonLayer.Fields)
                                {
                                    if (field.Domain != null && field.Domain.Type == "codedValue")
                                    {
                                        layer.Fields.Add(new Field(field.Name, field.Alias, FieldType.String)); // new DomainField ???
                                        layer.AddDomains(field.Name, field.Domain);
                                    }
                                    else
                                    {
                                        layer.Fields.Add(new Field(field.Name, field.Alias, RestHelper.FType(field.Type)));
                                    }
                                }
                            }

                            layer.ParentLayers = null;
                            if (jsonLayer.ParentLayer != null)
                            {
                                //layer.ParentLayers = layer.CalcParentLayerIds(int.Parse(jsonLayer.ParentLayer.Id.ToString()), jsonLayer.ParentLayer.Name, jsonLayers);
                                layer.CalcParentLayerIds(jsonLayer, jsonLayers.Layers);
                            }

                            layer.MinScale = jsonLayer.GetEffectiveMaxScale();
                            layer.MaxScale = jsonLayer.GetEffectiveMinScale();

                            layer.Description = jsonLayer.Description;

                            #region Views haben keine ID Spalte

                            if ((layer is FeatureLayer && (String.IsNullOrEmpty(layer.IdFieldName) || GeoServices.AXL.Globals.shortName(layer.IdFieldName).ToLower() == "fid")))
                            {
                                Layer.TrySetIdField(this.Map, this, layer);
                            }

                            #endregion

                            if (jsonLayer.DrawingInfo != null && jsonLayer.DrawingInfo.Renderer != null)
                            {
                                switch (jsonLayer.DrawingInfo.Renderer.Type?.ToLower())
                                {
                                    case "simple":
                                        layer.LengendRendererType = LayerRendererType.Simple;
                                        break;
                                    case "uniquevalue":
                                        layer.LengendRendererType = LayerRendererType.UniqueValue;
                                        layer.UniqueValue_Field1 = jsonLayer.DrawingInfo.Renderer.Field1;
                                        layer.UniqueValue_Field2 = jsonLayer.DrawingInfo.Renderer.Field2;
                                        layer.UniqueValue_Field3 = jsonLayer.DrawingInfo.Renderer.Field3;
                                        layer.UniqueValue_FieldDelimiter = jsonLayer.DrawingInfo.Renderer.FieldDelimiter.Trim();
                                        break;
                                }
                            }

                            ServiceHelper.SetLayerScale(this, layer);

                            layers.Add(layer);
                        }
                        else if (jsonLayer.Type == "Raster Layer" ||
                                 jsonLayer.Type == "Raster Catalog Layer" ||
                                 jsonLayer.Type == "Raster-Layer" ||
                                 jsonLayer.Type == "Raster-Catalog-Layer" ||
                                 jsonLayer.Type == "Mosaic Layer" ||
                                 jsonLayer.Type == "Mosaic-Layer")
                        {
                            RasterLayer layer = new RasterLayer(
                                layerName,
                                jsonLayer.Id.ToString(),
                                LayerType.image,
                                this,
                                queryable: true);

                            layer.ParentLayers = null;
                            if (jsonLayer.ParentLayer != null)
                            {
                                //layer.ParentLayers = layer.CalcParentLayerIds(int.Parse(jsonLayer.ParentLayer.Id.ToString()), jsonLayer.ParentLayer.Name, jsonLayers);
                                layer.CalcParentLayerIds(jsonLayer, jsonLayers.Layers);
                            }

                            layer.MinScale = jsonLayer.GetEffectiveMaxScale();
                            layer.MaxScale = jsonLayer.GetEffectiveMinScale();
                            layer.Visible = jsonLayer.DefaultVisibility;

                            ServiceHelper.SetLayerScale(this, layer);

                            layer.Description = jsonLayer.Description;

                            layers.Add(layer);
                        }
                        else
                        {
                            if (jsonLayer.ParentLayer != null && jsonLayer.ParentLayer.Type == "Annotation Layer")
                            {
                                AnnotationLayer layer = new AnnotationLayer(
                                    layerName,
                                    jsonLayer.Id.ToString(),
                                    LayerType.annotation,
                                    this);

                                layer.ParentLayers = null;
                                if (jsonLayer.ParentLayer != null)
                                {
                                    //layer.ParentLayers = layer.CalcParentLayerIds(int.Parse(jsonLayer.ParentLayer.Id.ToString()), jsonLayer.ParentLayer.Name, jsonLayers);
                                    layer.CalcParentLayerIds(jsonLayer, jsonLayers.Layers);
                                }

                                layer.MinScale = jsonLayer.GetEffectiveMaxScale();
                                layer.MaxScale = jsonLayer.GetEffectiveMinScale();
                                layer.Visible = jsonLayer.DefaultVisibility;

                                if (layer.MinScale <= 0)
                                {
                                    layer.MinScale = jsonLayer.ParentLayer.MaxScale;
                                }

                                if (layer.MaxScale <= 0)
                                {
                                    layer.MaxScale = jsonLayer.ParentLayer.MinScale;
                                }

                                ServiceHelper.SetLayerScale(this, layer);
                                layers.Add(layer);
                            }
                        }

                        if (jsonLayer.Extent != null && jsonLayer.Extent.IsInitialized())
                        {
                            if (this.InitialExtent == null)
                            {
                                this.InitialExtent = new Envelope(jsonLayer.Extent.Xmin, jsonLayer.Extent.Ymin, jsonLayer.Extent.Xmax, jsonLayer.Extent.Ymax);
                            }
                            else
                            {
                                this.InitialExtent.Union(new Envelope(jsonLayer.Extent.Xmin, jsonLayer.Extent.Ymin, jsonLayer.Extent.Xmax, jsonLayer.Extent.Ymax));
                            }
                        }
                    }

                    _layers.SetItems(layers);

                    #endregion
                }

                _maxImageWidth = jsonService.MaxImageWidth;
                _maxImageHeight = jsonService.MaxImageHeight;

                pLogger.Success = true;

                this.Diagnostics = ServiceTheme.CheckServiceLayers(this, _serviceThemes);

                if (this.Diagnostics != null && this.Diagnostics.State != ServiceDiagnosticState.Ok)
                {
                    _diagnosticsErrorResponse = new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, new Exception(this.Diagnostics.Message));
                    _errMsg = this.Diagnostics.Message;
                }

                return true;
            }
            catch (Exception ex)
            {
                _initErrorResponse = new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex, Const.InitServiceExceptionPreMessage);
                _errMsg = $"{ex.Message}\n{ex.StackTrace}";
                return false;
            }
        }

        //return true;
    }

    public bool IsDirty
    {
        get;
        set;
    }

    async public Task<ServiceResponse> GetMapAsync(IRequestContext requestContext)
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(this.Map?.Services != null ? this.Map.Services.IndexOf(this) : 0, this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, _mapServiceName, "GetMap", $"GetMap {this.Service}"))
        {
            if (!ServiceHelper.VisibleInScale(this, this.Map))
            {
                pLogger.Success = pLogger.SuppressLogging = true;
                return new EmptyImage(this.Map.Services.IndexOf(this), this.ID);
            }

            try
            {
                ErrorResponse innerErrorResponse = new ErrorResponse(this);
                if (this.Diagnostics != null && this.Diagnostics.ThrowExeption(this))
                {
                    if (_diagnosticsErrorResponse != null)
                    {
                        innerErrorResponse.AppendMessage(_diagnosticsErrorResponse);
                    }
                }

                bool hasVisibleLayers = false;

                List<DynamicLayer> dynamicLayers = new List<DynamicLayer>();
                bool useDynamicLayers = false;

                #region Layer Visibility/Layer Defintion Queries/Dynamic Layers

                List<string> visibleIds = new();
                Dictionary<string, string> layerDefs = new Dictionary<string, string>();

                foreach (var layer in this.Layers)
                {

                    if (layer != null)
                    {
                        bool visible = layer.Visible;

                        if (visible)
                        {
                            visibleIds.Add(layer.ID);
                        }

                        if (visible && layer is RestLayer)
                        {
                            hasVisibleLayers = true;
                        }

                        string where = String.Empty;
                        if (layer is FeatureLayer)
                        {
                            where = ((FeatureLayer)layer).DefinitionExpression;
                        }

                        if (!String.IsNullOrEmpty(layer.Filter))
                        {
                            where = where.AppendWhereClause(layer.Filter);
                        }

                        if (!String.IsNullOrWhiteSpace(where))
                        {
                            var layerDefLayerId = layer.ID;
                            if (layer.Type == LayerType.annotation && layer is RestLayer && ((RestLayer)layer).ParentLayerIds != null && ((RestLayer)layer).ParentLayerIds.Length > 0)
                            {
                                layerDefLayerId = ((RestLayer)layer).ParentLayerIds[0].ToString();
                            }
                            layerDefs.Add(layerDefLayerId, FeatureLayer.UrlEncodeWhere(where));
                        }

                        if (layer is FeatureLayer && ((FeatureLayer)layer).UseLabelRenderer && ((FeatureLayer)layer).LabelRenderer != null)
                        {
                            useDynamicLayers = true;

                            var labelRenderer = ((FeatureLayer)layer).LabelRenderer;
                            DynamicLayer dynamicLayer = new DynamicLayer();

                            dynamicLayer.id = 1001 + dynamicLayers.Count;
                            dynamicLayer.source = new DynamicLayerSouce()
                            {
                                mapLayerId = int.Parse(layer.ID)
                            };
                            dynamicLayer.drawingInfo = new DynamicLayerDrawingInfo()
                            {
                                showLabels = true
                            };
                            dynamicLayer.definitionExpression = FeatureLayer.UrlEncodeWhere(where);

                            var labelingInfo = new LabelingInfo()
                            {
                                LabelPlacement = LabelingInfo.DefaultLabelPlacement(layer.Type),
                                LabelExpression = $"[{labelRenderer.LabelField}]",
                                UseCodedValues = false,
                                MinScale = (int)layer.MaxScale,
                                MaxScale = (int)layer.MinScale,
                                Where = FeatureLayer.UrlEncodeWhere(where), //String.IsNullOrEmpty(layer.Filter) ? null : layer.Filter,
                                Symbol = new TextSymbol(labelRenderer)
                                {
                                    VerticalAlignment = "bottom",
                                    HorizontalAlignment = "left"
                                }
                            };

                            dynamicLayer.drawingInfo.labelingInfo = new LabelingInfo[] { labelingInfo };

                            dynamicLayers.Add(dynamicLayer);
                        }
                        else if (visible)
                        {
                            DynamicLayer dynamicLayer = new DynamicLayer();

                            dynamicLayer.id = 1001 + dynamicLayers.Count;
                            dynamicLayer.source = new DynamicLayerSouce()
                            {
                                mapLayerId = int.Parse(layer.ID)
                            };
                            dynamicLayer.definitionExpression = FeatureLayer.UrlEncodeWhere(where);  // Schreibweise nicht mehr gültig ab AGS 10.5
                            dynamicLayers.Add(dynamicLayer);
                        }
                    }
                }

                if (!hasVisibleLayers)
                {
                    pLogger.Success = pLogger.SuppressLogging = true;
                    return new EmptyImage(this.Map.Services.IndexOf(this), this.ID);
                }

                #endregion

                #region ImageDescription

                var imageSizeAndDpi = this.Map.CalcImageSizeAndDpi(_maxImageWidth, _maxImageHeight);
                if (imageSizeAndDpi.modified)
                {
                    innerErrorResponse.AppendWarningMessage($"Map image don't fit to maximum image resolution ({Map.ImageWidth}x{Map.ImageHeight})>({_maxImageWidth}x{_maxImageHeight})");
                }

                #endregion

                string requestUrl = $"{this.Service}/export";

                var requestBuilder = new ExportRequestBuilder()
                    .WithBBox(this.Map.Extent)
                    .WithLayers(this.SealedLayers
                           ? null
                           : visibleIds)
                    .WithLayerDefintions(layerDefs)
                    .WithMapRotation(this.Map)
                    .WithImageFormat(_imageFormat)
                    .WithTransparency()
                    .WithImageSizeAndDpi(imageSizeAndDpi.imageWidth, imageSizeAndDpi.imageHeight, (int)imageSizeAndDpi.dpi)
                    .WithImageAndBBoxSRef(
                        this.ProjectionMethode switch
                        {
                            ServiceProjectionMethode.Map => Map.SpatialReference?.Id ?? 0,
                            ServiceProjectionMethode.Userdefined => this.ProjectionId,
                            _ => 0
                        })
                    .WithDatumTransformations(this.DatumTransformations)
                    .WithDynamicLayers(
                        useDynamicLayers switch
                        {
                            false => null,
                            _ => dynamicLayers
                        }
                    );


                if (this.ExportMapFormat == AGSExportMapFormat.Image)
                {
                    requestBuilder.WithFormat("image");

                    #region Image Response

                    byte[] result = await authHandler.TryPostRawAsync(this, requestUrl, requestBuilder.Build());

                    if (result == null || result.Length == 0)
                    {
                        return new ErrorResponse(
                                this.Map.Services.IndexOf(this),
                                this.ID, "No imageData returned", "");
                    }

                    string imageExtension = "png";
                    if (_imageFormat.StartsWith("png"))
                    {
                        imageExtension = "png";
                    }
                    else if (_imageFormat.StartsWith("jpg") || _imageFormat.StartsWith("jpeg"))
                    {
                        imageExtension = "jpg";
                    }
                    string filename = $"ags{Guid.NewGuid().ToString("N").ToLower()}.{imageExtension}";
                    string filePath = System.IO.Path.Combine(this.Map.Environment.UserString(webgisConst.OutputPath), filename);
                    string fileUrl = $"{(string)this.Map.Environment.UserValue(webgisConst.OutputUrl, String.Empty)}/{filename}";

                    await result.SaveOrUpload(filePath);

                    pLogger.Success = true;
                    return new ImageLocation(this.Map.Services.IndexOf(this), this.ID,
                        filePath, fileUrl)
                    {
                        InnerErrorResponse = innerErrorResponse.HasErrors ? innerErrorResponse : null
                    };

                    #endregion
                }
                else
                {
                    requestBuilder.WithFormat("json");

                    #region Json Response

                    string result = await requestContext.LogRequest(
                        this.Service,
                        this.ServiceShortname,
                        requestBuilder.Build(),
                        "export_map",
                        (requestBody) => authHandler.TryPostAsync(
                            this, 
                            requestUrl, 
                            requestBody));

                    var jsonResult = JSerializer.Deserialize<JsonExportResponse>(result);

                    var extent = jsonResult.Extent != null ? new Envelope(jsonResult.Extent.Xmin, jsonResult.Extent.Ymin, jsonResult.Extent.Xmax, jsonResult.Extent.Ymax) : null;
                    var scale = jsonResult.Scale;

                    pLogger.Success = true;
                    if (!String.IsNullOrEmpty(jsonResult.Href))
                    {
                        string imageUrl = requestContext.Http.ApplyUrlOutputRedirection(jsonResult.Href);

                        return new ImageLocation(
                            this.Map.Services.IndexOf(this),
                            this.ID,
                            String.Empty,
                            imageUrl)
                        {
                            InnerErrorResponse = innerErrorResponse.HasErrors ? innerErrorResponse : null,
                            Extent = extent,
                            Scale = scale
                        };
                    }
                    else if (!String.IsNullOrEmpty(jsonResult.ImageData))
                    {
                        string fileName = $"ags_{Guid.NewGuid().ToString("N").ToLower()}.{(jsonResult.ContentType == "image/png" ? "png" : "jpg")}";
                        using (StreamWriter sw = new StreamWriter(System.IO.Path.Combine(this.Map.Environment.UserString(webgisConst.OutputPath), fileName)))
                        {
                            BinaryWriter bw = new BinaryWriter(sw.BaseStream);
                            bw.Write(Convert.FromBase64String(jsonResult.ImageData));
                        }

                        pLogger.Success = true;
                        return new ImageLocation(
                            this.Map.Services.IndexOf(this),
                            this.ID,
                            System.IO.Path.Combine(this.Map.Environment.UserString(webgisConst.OutputPath), fileName),
                            $"{this.Map.Environment.UserString(webgisConst.OutputUrl)}/{fileName}")
                        {
                            InnerErrorResponse = innerErrorResponse.HasErrors ? innerErrorResponse : null,
                            Extent = extent,
                            Scale = scale
                        };
                    }
                    else
                    {
                        return new ErrorResponse(
                            this.Map.Services.IndexOf(this),
                            this.ID, "No imageData or href returned", "");
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                return new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex);
            }
        }
    }

    async public Task<ServiceResponse> GetSelectionAsync(SelectionCollection selectionCollection, IRequestContext requestContext)
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(this.Map?.Services != null ? this.Map.Services.IndexOf(this) : 0, this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

        using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, _mapServiceName, "GetSelection", $"GetSelection {this.Service}"))
        {
            try
            {
                string dynamicLayerRequestUrl =
                    $"{this.Service}/export";

                StringBuilder dynamicLayersDefinition = new StringBuilder();
                dynamicLayersDefinition.Append("[");

                var dpiFactor = 1.0D;

                List<DynamicLayer> dynamicLayers = new List<DynamicLayer>();

                foreach (var selection in selectionCollection)
                {
                    if (selection.Layer.Service.ID == this.ID)
                    {
                        if (selectionCollection.Count == 1 &&
                            !String.IsNullOrEmpty(selection.Filter.Where) &&
                            selection.Filter.Where.StartsWith($"{selection.Layer.IdFieldName} in ("))
                        {
                            //
                            // for single selection with object id filter, allow to change the dpiFactor
                            // => the selection is also visible if it is out of max scale range
                            //
                            if (selection.Layer.MaxScale > 0 && selection.Layer.MaxScale < Map.MapScale)
                            {
                                dpiFactor = Map.MapScale / selection.Layer.MaxScale;
                            }
                        }

                        QueryFilter filter = selection.Filter;

                        string where = filter.Where;
                        if (selection.Layer != null && !String.IsNullOrEmpty(selection.Layer.Filter))
                        {
                            where = where.AppendWhereClause(selection.Layer.Filter);
                        }

                        int[] selectionColor = new int[4] { selection.Color.R, selection.Color.G, selection.Color.B, selection.Color.A };
                        int[] selectionColor80 = new int[4] { selection.Color.R, selection.Color.G, selection.Color.B, selection.Color.A * 8 / 10 };
                        int[] selectionColor33 = new int[4] { selection.Color.R, selection.Color.G, selection.Color.B, selection.Color.A / 3 };

                        object symbol = selection.Layer.Type switch
                        {
                            LayerType.point =>
                                new SimpleMarkerSymbol()
                                {
                                    Type = "esriSMS",
                                    Style = "esriSMSCircle",
                                    Color = selectionColor,
                                    Size = 8,
                                    Angle = 0,
                                    Xoffset = 0,
                                    Yoffset = 0,
                                    Outline = new Outline()
                                    {
                                        Type = "",
                                        Style = "",
                                        Color = selectionColor,
                                        Width = 10 * (float)dpiFactor
                                    }
                                },
                            LayerType.line =>
                                new SimpleLineSymbol()
                                {
                                    Type = "esriSLS",
                                    Style = "esriSLSSolid",
                                    Color = selectionColor80,
                                    Width = 4 * (float)dpiFactor
                                },
                            LayerType.polygon =>
                                new SimpleFillSymbol()
                                {
                                    Type = "esriSFS",
                                    Style = "esriSFSSolid", // Schraffur
                                    Color = selectionColor33,
                                    Outline = new Outline()
                                    {
                                        Type = "esriSFS",
                                        Style = "esriSLSSolid", // Border
                                        Color = selectionColor80,
                                        Width = 2 * (float)dpiFactor
                                    }
                                },
                            _ => throw new NotImplementedException($"Type {selection.Layer.Type} is not implemented.")
                        };

                        dynamicLayers.Add(new DynamicLayer()
                        {
                            id = 1001,
                            source = new DynamicLayerSouce()
                            {
                                type = "maplayer",
                                mapLayerId = int.Parse(selection.Layer.ID)
                            },
                            definitionExpression = where.Replace("%", "%25"),
                            drawingInfo = new DynamicLayerDrawingInfo()
                            {
                                labelingInfo = [],
                                renderer = new
                                {
                                    type = "simple",
                                    symbol = symbol
                                },
                                transparency = 0,
                            }
                        });
                    }
                }

                var requestBuilder = new ExportRequestBuilder()
                    .WithBBox(this.Map.Extent)
                    .WithLayers(_layers.Select(l => l.ID), "hide")  // hide all layers
                    .WithLayerDefintions(null)
                    .WithImageFormat("png32")
                    .WithTransparency()
                    .WithImageSizeAndDpi(this.Map.ImageWidth, this.Map.ImageHeight, (int)(this.Map.Dpi / dpiFactor))
                    .WithMapRotation(this.Map)
                    .WithImageAndBBoxSRef(
                        this.ProjectionMethode switch
                        {
                            ServiceProjectionMethode.Map => Map.SpatialReference?.Id ?? 0,
                            ServiceProjectionMethode.Userdefined => this.ProjectionId,
                            _ => 0
                        })
                    .WithDatumTransformations(this.DatumTransformations)
                    .WithDynamicLayers(dynamicLayers);

                if (this.ExportMapFormat == AGSExportMapFormat.Image)
                {
                    #region Image Response

                    requestBuilder.WithFormat("image");

                    byte[] result = await authHandler.TryPostRawAsync(this, dynamicLayerRequestUrl, requestBuilder.Build());

                    if (result == null || result.Length == 0)
                    {
                        return new ErrorResponse(
                                this.Map.Services.IndexOf(this),
                                this.ID, "No imageData returned", "");
                    }

                    string imageExtension = "png";
                    if (_imageFormat.StartsWith("png"))
                    {
                        imageExtension = "png";
                    }
                    else if (_imageFormat.StartsWith("jpg") || _imageFormat.StartsWith("jpeg"))
                    {
                        imageExtension = "jpg";
                    }
                    string filename = $"ags{Guid.NewGuid().ToString("N").ToLower()}.{imageExtension}";
                    string filePath = System.IO.Path.Combine(this.Map.Environment.UserString(webgisConst.OutputPath), filename);
                    string fileUrl = $"{(string)this.Map.Environment.UserValue(webgisConst.OutputUrl, String.Empty)}/{filename}";

                    await result.SaveOrUpload(filePath);

                    ErrorResponse innerErrorResponse = new ErrorResponse(this);
                    if (this.Diagnostics != null && this.Diagnostics.ThrowExeption(this))
                    {
                        if (_diagnosticsErrorResponse != null)
                        {
                            innerErrorResponse.AppendMessage(_diagnosticsErrorResponse);
                        }
                    }

                    pLogger.Success = true;
                    return new ImageLocation(this.Map.Services.IndexOf(this), this.ID,
                        filePath, fileUrl)
                    {
                        InnerErrorResponse = innerErrorResponse.HasErrors ? innerErrorResponse : null
                    };

                    #endregion
                }
                else
                {
                    #region Json Response

                    requestBuilder.WithFormat("json");

                    string dynamicLayerResponse = await authHandler.TryPostAsync(this, dynamicLayerRequestUrl, requestBuilder.Build());
                    var jsonResult = JSerializer.Deserialize<JsonExportResponse>(dynamicLayerResponse);

                    var extent = jsonResult?.Extent != null ? new Envelope(jsonResult.Extent.Xmin, jsonResult.Extent.Ymin, jsonResult.Extent.Xmax, jsonResult.Extent.Ymax) : null;
                    var scale = jsonResult.Scale;

                    pLogger.Success = true;
                    if (!String.IsNullOrEmpty(jsonResult.Href))
                    {
                        string imageUrl = requestContext.Http.ApplyUrlOutputRedirection(jsonResult.Href);

                        return new ImageLocation(
                            this.Map.Services.IndexOf(this),
                            this.ID,
                            String.Empty,
                            imageUrl)
                        {
                            Extent = extent,
                            Scale = scale
                        };
                    }
                    else if (!String.IsNullOrEmpty(jsonResult.ImageData))
                    {
                        string fileName = $"ags_selection_{Guid.NewGuid().ToString("N").ToLower()}.{(jsonResult.ContentType == "image/png" ? "png" : "jpg")}";
                        using (StreamWriter sw = new StreamWriter(System.IO.Path.Combine(this.Map.Environment.UserString(webgisConst.OutputPath), fileName)))
                        {
                            BinaryWriter bw = new BinaryWriter(sw.BaseStream);
                            bw.Write(Convert.FromBase64String(jsonResult.ImageData));
                            sw.Flush();
                            sw.Close();
                        }

                        pLogger.Success = true;
                        return new ImageLocation(
                            this.Map.Services.IndexOf(this),
                            this.ID,
                            System.IO.Path.Combine(this.Map.Environment.UserString(webgisConst.OutputPath), fileName),
                            $"{this.Map.Environment.UserString(webgisConst.OutputUrl)}/{fileName}")
                        {
                            Extent = extent,
                            Scale = scale
                        };
                    }
                    else
                    {
                        return new ErrorResponse(
                            this.Map.Services.IndexOf(this),
                            this.ID, "No imageData or href returned", "");
                    }

                    #endregion
                }
            }
            catch (Exception ex)
            {
                requestContext
                    .GetRequiredService<IExceptionLogger>()
                    .LogException(this.Map, this.Server, _mapServiceName, "GetSelection", ex);

                return new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex);
            }
        }
    }

    public int Timeout
    {
        get; set;
        //get
        //{
        //    return _httpRequest.Timeout / 1000;
        //}
        //set
        //{
        //    _httpRequest.Timeout = value * 1000;
        //}
    }

    public IMap Map
    {
        get;
        private set;
    }

    public double MinScale
    {
        get;
        set;
    }

    public double MaxScale
    {
        get;
        set;
    }

    public bool ShowInToc { get; set; }

    public string CollectionId
    {
        get;
        set;
    }

    public bool CheckSpatialConstraints
    {
        get;
        set;
    }

    public bool IsBaseMap
    {
        get;
        set;
    }

    public BasemapType BasemapType
    {
        get;
        set;
    }

    public string BasemapPreviewImage { get; set; }

    #endregion

    #region IService2 Member

    public ServiceResponse PreGetMap()
    {
        if (_initErrorResponse != null)
        {
            return new ErrorResponse(this.Map.Services.IndexOf(this), this.ID, _initErrorResponse.ErrorMessage, _initErrorResponse.ErrorMessage2);
        }

        if (!ServiceHelper.VisibleInScale(this, this.Map))
        {
            return new EmptyImage(this.Map.Services.IndexOf(this), this.ID);
        }

        bool hasVisibleLayers = false;
        foreach (var layer in this.Layers)
        {
            if (layer != null)
            {
                bool visible = layer.Visible;

                if (visible && layer is RestLayer)
                {
                    hasVisibleLayers = true;
                    break;
                }
            }
        }

        if (!hasVisibleLayers)
        {
            return new EmptyImage(this.Map.Services.IndexOf(this), this.ID);
        }

        return null;
    }

    public IEnumerable<ILayerProperties> LayerProperties { get; set; }

    private ServiceTheme[] _themes = null;
    public IEnumerable<ServiceTheme> ServiceThemes
    {
        get { return _themes; }
        set { _themes = value?.ToArray(); }
    }

    #endregion

    #region IService3 Members

    public string FuzzyLayerNameSeperator
    {
        get { return "\\"; }
    }

    #endregion

    #region IDymamicService

    public ServiceDynamicPresentations CreatePresentationsDynamic { get; set; }
    public ServiceDynamicQueries CreateQueriesDynamic { get; set; }

    #endregion

    #region IImageServiceType 

    public ImageServiceType ImageServiceType { get; set; }

    #endregion

    #region IClone Members

    public IMapService Clone(IMap parent)
    {
        MapService clone = new MapService();

        clone.Map = parent ?? this.Map;

        clone.Name = this.Name;
        clone.Url = this.Url;
        clone.Server = this.Server;
        clone.Service = this.Service;
        clone._mapServiceName = this._mapServiceName;
        clone.ID = this.ID;
        clone.UseToc = this.UseToc;
        clone.InitialExtent = this.InitialExtent;
        clone.Opacity = this.Opacity;
        clone.ShowInToc = this.ShowInToc;

        clone.MinScale = this.MinScale;
        clone.MaxScale = this.MaxScale;
        clone.CollectionId = this.CollectionId;
        clone.CheckSpatialConstraints = this.CheckSpatialConstraints;
        clone.IsBaseMap = this.IsBaseMap;
        clone.BasemapType = this.BasemapType;
        clone.BasemapPreviewImage = this.BasemapPreviewImage;

        clone.TokenExpiration = this.TokenExpiration;
        clone.Timeout = this.Timeout;

        foreach (ILayer layer in _layers)
        {
            if (layer == null)
            {
                continue;
            }

            clone._layers.Add(layer.Clone(clone));
        }

        clone.ShowServiceLegendInMap = this.ShowServiceLegendInMap;
        clone.LegendVisible = this.LegendVisible;
        clone.LegendOptMethod = this.LegendOptMethod;
        clone.LegendOptSymbolScale = this.LegendOptSymbolScale;
        clone.FixLegendUrl = this.FixLegendUrl;

        clone.ProjectionMethode = this.ProjectionMethode;
        clone.ProjectionId = this.ProjectionId;
        clone.DatumTransformations = this.DatumTransformations != null && this.DatumTransformations.Length > 0
            ? this.DatumTransformations.Select(d => d).ToArray()
            : null;
        clone.HttpCredentials = this.HttpCredentials;
        clone.Username = this.Username;
        clone.Password = this.Password;
        clone.StaticToken = this.StaticToken;

        clone._imageFormat = this._imageFormat;
        clone._initErrorResponse = _initErrorResponse;
        clone._diagnosticsErrorResponse = _diagnosticsErrorResponse;

        clone._exportWms = _exportWms;
        clone._ogcEnvelope = _ogcEnvelope != null ? new Envelope(_ogcEnvelope) : null;

        clone._maxImageWidth = this._maxImageWidth;
        clone._maxImageHeight = this._maxImageHeight;

        clone.SupportedCrs = this.SupportedCrs;
        clone._serviceThemes = _serviceThemes;
        clone.Diagnostics = this.Diagnostics;
        clone.DiagnosticsWaringLevel = this.DiagnosticsWaringLevel;

        clone.CopyrightInfoId = this.CopyrightInfoId;

        clone.ServiceDescription = this.ServiceDescription;
        clone.CopyrightText = this.CopyrightText;

        clone.ExportMapFormat = this.ExportMapFormat;

        clone.LayerProperties = this.LayerProperties;

        clone.CreatePresentationsDynamic = this.CreatePresentationsDynamic;
        clone.CreateQueriesDynamic = this.CreateQueriesDynamic;
        clone.ImageServiceType = this.ImageServiceType;

        clone.ServiceThemes = this.ServiceThemes;
        clone.SealedLayers = this.SealedLayers;

        return clone;
    }

    #endregion

    #region IServiceSelectionProperties Members

    public bool CanMultipleSelections
    {
        get { return false; }
    }

    #endregion

    #region IServiceProjection Members

    public ServiceProjectionMethode ProjectionMethode
    {
        get;
        set;
    }

    public int ProjectionId
    {
        get;
        set;
    }

    public void RefreshSpatialReference()
    {
        //switch (_projMethode)
        //{
        //    case ServiceProjectionMethode.Map:
        //        if (this.Map.SpatialReference != null && this.Map.SpatialReference.Id > 0 &&
        //            _mapDescription != null && _mapDescription.SpatialReference != null && !String.IsNullOrEmpty(this.Map.SpatialReference.WKT))
        //        {
        //            _mapDescription.SpatialReference.WKID = this.Map.SpatialReference.Id;
        //            _mapDescription.SpatialReference.WKIDSpecified = true;
        //            _mapDescription.SpatialReference.WKT = this.Map.SpatialReference.WKT;
        //            if (_selectionDescription != null)
        //            {
        //                _selectionDescription.SpatialReference.WKID = this.Map.SpatialReference.Id;
        //                _selectionDescription.SpatialReference.WKIDSpecified = true;
        //                _selectionDescription.SpatialReference.WKT = this.Map.SpatialReference.WKT;
        //            }
        //        }
        //        break;
        //    case ServiceProjectionMethode.Userdefined:
        //        if (_projId > 0)
        //        {
        //            E.WebMapping.Geometry.SpatialReference sref = this.Map.MapSession.MapApplication.SpatialReferences.ById(_projId);
        //            if (_mapDescription != null && _mapDescription.SpatialReference != null && sref != null && !String.IsNullOrEmpty(sref.WKT))
        //            {
        //                _mapDescription.SpatialReference.WKID = _projId;
        //                _mapDescription.SpatialReference.WKIDSpecified = true;
        //                _mapDescription.SpatialReference.WKT = sref.WKT;
        //                if (_selectionDescription != null)
        //                {
        //                    _selectionDescription.SpatialReference.WKID = _projId;
        //                    _selectionDescription.SpatialReference.WKIDSpecified = true;
        //                    _selectionDescription.SpatialReference.WKT = sref.WKT;
        //                }
        //            }
        //        }
        //        break;
        //}
    }

    #endregion

    #region IServiceDatumTransformations

    public int[] DatumTransformations { get; set; }

    #endregion

    #region IServiceLegend Members

    bool _legendVisible = false;
    public bool LegendVisible
    {
        get
        {
            return _legendVisible;
        }
        set
        {
            _legendVisible = value;
        }
    }

    async public Task<ServiceResponse> GetLegendAsync(IRequestContext requestContext)
    {
        try
        {
            if (!_legendVisible)
            {
                return new ServiceResponse(this.Map.Services.IndexOf(this), this.ID);
            }

            if (!String.IsNullOrEmpty(FixLegendUrl))
            {
                return new ImageLocation(this.Map.Services.IndexOf(this),
                    this.ID, String.Empty, FixLegendUrl);
            }

            using (var pLogger = requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Start(this.Map, this.Server, _mapServiceName, "GetLegend", $"GetLegend {this.Service}"))
            {
                var httpService = requestContext.Http;
                var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

                string dynamicLayerRequestUrl = $"{this.Service}/legend";
                string jsonAnswer = await authHandler.TryPostAsync(
                        this,
                        dynamicLayerRequestUrl,
                        LegendRequestBuilder.DefaultRequest);

                return await this.RenderRestLegendResponse(requestContext, jsonAnswer);
            }
        }
        catch (Exception ex)
        {
            return new ExceptionResponse(this.Map.Services.IndexOf(this), this.ID, ex);
        }
    }

    private bool _showServiceLegendInMap = true;
    public bool ShowServiceLegendInMap
    {
        get
        {
            return _showServiceLegendInMap;
        }
        set
        {
            _showServiceLegendInMap = value;
        }
    }

    private double _legendOptSymbolScale = 1000.0;
    public double LegendOptSymbolScale
    {
        get
        {
            return _legendOptSymbolScale;
        }
        set
        {
            _legendOptSymbolScale = value;
        }
    }

    private LegendOptimization _legendOptMethod = LegendOptimization.None;
    public LegendOptimization LegendOptMethod
    {
        get
        {
            return _legendOptMethod;
        }
        set
        {
            _legendOptMethod = value;
        }
    }

    private string _fixLegendUrl = String.Empty;
    public string FixLegendUrl
    {
        get
        {
            return _fixLegendUrl;
        }
        set
        {
            _fixLegendUrl = value;
        }
    }

    #endregion

    #region IServiceLegend2

    async public Task<IEnumerable<LayerLegendItem>> GetLayerLegendItemsAsync(string layerId, IRequestContext requestContext)
    {
        //if (!String.IsNullOrEmpty(FixLegendUrl))
        //{
        //    return new LayerLegendItem[0];
        //}
        try
        {
            string dynamicLayerRequestUrl = this.Service + "/legend";

            var authHandler = requestContext.GetRequiredService<AgsAuthenticationHandler>();

            string jsonAnswer = await authHandler.TryPostAsync(
                        this,
                        dynamicLayerRequestUrl,
                        LegendRequestBuilder.DefaultRequest);
            var legendResponse = JSerializer.Deserialize<Legend.LegendResponse>(jsonAnswer);

            var layer = legendResponse.Layers.Where(m => m.LayerId.ToString() == layerId).FirstOrDefault();
            if (layer == null || layer.Legend == null)
            {
                return new LayerLegendItem[0];
            }

            List<LayerLegendItem> items = new List<LayerLegendItem>();
            foreach (var legend in layer.Legend)
            {
                items.Add(new LayerLegendItem()
                {
                    Label = legend.Label,
                    ContentType = legend.ContentType,
                    Width = legend.Width,
                    Height = legend.Height,
                    Data = Convert.FromBase64String(legend.ImageData),
                    Values = legend.Values
                });
            }
            return items;
        }
        catch { }
        return new LayerLegendItem[0];
    }

    #endregion

    #region IExportableOgcService Member

    private bool _exportWms = false;
    public bool ExportWms
    {
        get { return _exportWms; }
        set { _exportWms = value; }
    }

    private Envelope _ogcEnvelope = null;
    public Envelope OgcEnvelope
    {
        get { return _ogcEnvelope; }
        set { _ogcEnvelope = value; }
    }

    #endregion

    #region IServiceCopyrightInfo 

    public string CopyrightInfoId { get; set; }

    #endregion

    #region IFeatureWorkspaceProvider

    public IFeatureWorkspace GetFeatureWorkspace(string layerId)
    {
        var workspace = new FeatureService(this);

        var connectionString = new StringBuilder();

        connectionString.Append($"service={this.Service}"
            .Replace("/MapServer", "/FeatureServer")
            .Replace("/mapserver", "/featureserver"));

        connectionString.Append($"/{layerId}/");

        if (!String.IsNullOrWhiteSpace(this.Username) && !String.IsNullOrWhiteSpace(this.Password))
        {
            connectionString.Append($";user={this.Username};pwd={this.Password}");
        }

        workspace.ConnectionString = connectionString.ToString();

        return workspace;
    }

    #endregion

    public void SetImageFormat(ServiceImageFormat imageFormat)
    {
        switch (imageFormat)
        {
            case ServiceImageFormat.GIF:
                _imageFormat = "gif";
                break;
            case ServiceImageFormat.JPG:
                _imageFormat = "jpg";
                break;
            case ServiceImageFormat.PNG24:
                _imageFormat = "png24";
                break;
            case ServiceImageFormat.PNG32:
                _imageFormat = "png32";
                break;
            case ServiceImageFormat.PNG8:
                _imageFormat = "png8";
                break;
            default:
                _imageFormat = "png";
                break;
        }
    }

    #region IMapServiceAuthentication

    public string Username { get; private set; }
    public string Password { get; private set; }
    public string StaticToken { get; private set; }

    public int TokenExpiration
    {
        get;
        set;
    }

    public ICredentials HttpCredentials { get; set; }

    #endregion

    public AGSExportMapFormat ExportMapFormat { get; set; }

    public bool SealedLayers { get; set; } = false;

    #region IServiceSupportedCrs Member

    public int[] SupportedCrs
    {
        get;
        set;
    }

    #endregion

    #region IServiceDescription

    public string ServiceDescription { get; set; }
    public string CopyrightText { get; set; }

    #endregion

    #region IServiceInitialException

    public ErrorResponse InitialException => _initErrorResponse;

    #endregion

    #region Helper

    private string MapServiceName(string url)
    {
        var p = url.Split('/');
        if (p.Length > 1 && p[p.Length - 1].ToLower() == "mapserver")
        {
            return p[p.Length - 2];
        }

        return p[p.Length - 1];
    }

    #endregion
}
