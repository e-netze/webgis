#pragma warning disable CA1416

using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Extensions;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Core;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.Graphics;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;
using E.Standard.WebMapping.GeoServices.Tiling;
using gView.GraphicsEngine;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestMappingHelperService /*: IDisposable*/
{
    private readonly RestHelperService _restHelper;
    private readonly HttpRequestContextService _httpRequestContext;
    private readonly CacheService _cache;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly UrlHelperService _urlHelper;
    private readonly ICryptoService _crypto;
    private readonly IRequestContext _requestContext;

    public RestMappingHelperService(RestHelperService restHelper,
                                    HttpRequestContextService httpRequestContext,
                                    CacheService cache,
                                    MapServiceInitializerService mapServiceInitializer,
                                    UrlHelperService urlHelper,
                                    ICryptoService crypto,
                                    IRequestContext requestContext)
    {
        _restHelper = restHelper;
        _httpRequestContext = httpRequestContext;
        _cache = cache;
        _mapServiceInitializer = mapServiceInitializer;
        _urlHelper = urlHelper;
        _crypto = crypto;
        _requestContext = requestContext;
    }

    #region Map/Tile/Legend

    async public Task<object> PerformGetMap(HttpContext httpContext, string id, CmsDocument.UserIdentification ui)
    {
        var watch = new StopWatch(id);
        var request = httpContext.Request;

        string requestid = request.FormOrQuery("requestid");

        IMap map = _mapServiceInitializer.Map(_requestContext, ui, request.MapName());
        map.RequestId = !String.IsNullOrEmpty(requestid) ? requestid : map.RequestId;

        var getServiceResult = await _cache.GetServiceAndLayerProperties(id, map, ui, _urlHelper);
        if (getServiceResult.service == null)
        {
            getServiceResult = (service: await _mapServiceInitializer.GetCustomServiceByUrlAsync(id, map, ui, httpContext?.Request?.FormCollection()), serviceLayerProperties: new LayerPropertiesDTO[0]);
        }

        IMapService service = getServiceResult.service;
        if (service == null)
        {
            throw new Exception("Unknown service: " + id);
        }

        map.ImageWidth = int.Parse(request.FormOrQuery("width"));
        map.ImageHeight = int.Parse(request.FormOrQuery("height"));
        map.AddTimeEpoch(id, request.FormOrQuery("time_epoch")?.UrlParameterToTimeEpoch());

        string[] bbox = request.FormOrQuery("bbox").ToString().Split(',');
        string[] layerIds = request.FormOrQuery("layers")?.ToString().Split(',') ?? new string[0];

        string crsId = request.FormOrQuery("crs");
        if (!String.IsNullOrEmpty(crsId))
        {
            if (crsId.ToLower().StartsWith("epsg:"))
            {
                crsId = crsId.Split(':')[1];
            }

            map.SpatialReference = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(int.Parse(crsId));
            if (service is IMapServiceProjection)
            {
                ((IMapServiceProjection)service).ProjectionMethode = ServiceProjectionMethode.Map;
                ((IMapServiceProjection)service).RefreshSpatialReference();
            }
        }

        #region Layer Visibility

        var serviceLayerProperites = getServiceResult.serviceLayerProperties;
        var unauthorizedLayers = _cache.GetUnauthorizedLayerIds(service, ui);

        if (service.Layers != null)
        {
            foreach (var layer in service.Layers)
            {
                layer.Visible = false;
            }
        }

        foreach (string layerId in layerIds)
        {
            var layer = service.Layers.FindByLayerId(layerId);
            if (layer != null && !unauthorizedLayers.Contains(layer.ID))
            {
                layer.Visible = true;
            }
        }

        #region Locked Layers

        ApplyLockedLayers(service, serviceLayerProperites);

        #endregion

        #endregion

        #region VisFilters

        ApplyVisFilters(httpContext.Request, service, ui);

        #endregion

        #region Labeling

        var labelingDefinitions = httpContext.Request.LabelDefinitionsFromParameters();

        service.AddLabeling(labelingDefinitions, _cache, ui);

        #endregion

        #region dpi

        if (!String.IsNullOrEmpty(request.FormOrQuery("dpi")))
        {
            map.Dpi = request.FormOrQuery("dpi").ToPlatformDouble();
        }

        #endregion

        map.ZoomTo(new Envelope(
                bbox[0].ToPlatformDouble(),
                bbox[1].ToPlatformDouble(),
                bbox[2].ToPlatformDouble(),
                bbox[3].ToPlatformDouble()));

        if (!String.IsNullOrWhiteSpace(request.FormOrQuery("scale")))
        {
            map.SetScale(request.FormOrQuery("scale").ToPlatformDouble(), map.ImageWidth, map.ImageHeight);
        }

        ServiceResponse response = null;
        switch (service.ResponseType)
        {
            case ServiceResponseType.Image:
                response = await service.GetMapAsync(_requestContext);
                break;
            case ServiceResponseType.Html:
                if (service is IPrintableMapService)
                {
                    response = await ((IPrintableMapService)service).GetPrintMapAsync(_requestContext);
                }

                break;
        }

        if (response == null)
        {
            throw new Exception("No response");
        }

        if (response is ImageLocation imageResponse)
        {
            return watch.Apply(new ImageLocationResponseDTO()
            {
                id = id,
                url = imageResponse.ImageUrl,
                Path = imageResponse.ImagePath,
                requestid = requestid,
                scale = imageResponse.Scale,
                extent = imageResponse.Extent != null ?
                    new double[] { imageResponse.Extent.MinX, imageResponse.Extent.MinY, imageResponse.Extent.MaxX, imageResponse.Extent.MaxY } :
                    null,
                ErrorMesssage = imageResponse.InnerErrorResponse?.ErrorMessage
            });
        }

        if (response is ErrorResponse errorResponse)
        {
            errorResponse.ThrowException(
                _urlHelper.AppRootUrl(HttpSchema.Default),
                id,
                "GetMap",
                requestid,
                true);
        }

        throw new Exception("Unhandeld response type: " + response.GetType());
    }

    async public Task<object> PerformGetSelection(HttpContext httpContext, string id, CmsDocument.UserIdentification ui)
    {
        var request = httpContext.Request;
        var watch = new StopWatch(id);

        IMap map = _mapServiceInitializer.Map(_requestContext, ui, request.MapName());

        IMapService service = await _cache.GetService(id, map, ui, _urlHelper) ?? await _mapServiceInitializer.GetCustomServiceByUrlAsync(id, map, ui, httpContext?.Request?.FormCollection());
        if (service == null)
        {
            throw new Exception("Unknown service: " + id);
        }

        map.ImageWidth = int.Parse(request.FormOrQuery("width"));
        map.ImageHeight = int.Parse(request.FormOrQuery("height"));
        map.AddTimeEpoch(id, request.FormOrQuery("time_epoch")?.UrlParameterToTimeEpoch());
        string[] bbox = request.FormOrQuery("bbox").ToString().Split(',');

        map.ZoomTo(new Envelope(
                    bbox[0].ToPlatformDouble(),
                    bbox[1].ToPlatformDouble(),
                    bbox[2].ToPlatformDouble(),
                    bbox[3].ToPlatformDouble()));

        string requestid = request.FormOrQuery("requestid");

        string crsId = request.FormOrQuery("crs");
        if (!String.IsNullOrEmpty(crsId))
        {
            if (crsId.ToLower().StartsWith("epsg:"))
            {
                crsId = crsId.Split(':')[1];
            }

            map.SpatialReference = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(int.Parse(crsId));
            if (service is IMapServiceProjection)
            {
                ((IMapServiceProjection)service).ProjectionMethode = ServiceProjectionMethode.Map;
            }
        }

        ServiceResponse response = null;

        if (!String.IsNullOrEmpty(request.FormOrQuery("customid")))
        {
            #region Custom Selection

            // 
            // Selection liegt als FeatureCollection als GeoJson im Output Verzeichnis => zb Bufferfläche
            //

            string customId = request.FormOrQuery("customid");
            FileInfo fi = new FileInfo($"{_urlHelper.OutputPath()}/{customId}.json");
            if (fi.Exists)
            {
                var geoJsonFeatures = JSerializer.Deserialize<FeaturesDTO>(await File.ReadAllTextAsync(fi.FullName));

                if (geoJsonFeatures?.features != null)
                {
                    using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences.ById(4326), map.SpatialReference))
                    {
                        foreach (var geoJsonFeature in geoJsonFeatures.features)
                        {
                            TryAddToGraphicsContainer(map, geoJsonFeature, transformer);
                        }

                        if (map.GraphicsContainer.Count() > 0)
                        {
                            var graphicsService = new GraphicsService();
                            await graphicsService.InitAsync(map, _requestContext);
                            map.Services.Add(graphicsService);
                        }
                    }
                }

                response = await map.GetMapAsync(_requestContext, makeTransparent: true, format: "png");
            }

            #endregion
        }
        else
        {
            #region Service Selection

            string layerId = String.Empty;
            if (!String.IsNullOrWhiteSpace(request.FormOrQuery("layer")))
            {
                layerId = request.FormOrQuery("layer");
            }
            else if (!String.IsNullOrWhiteSpace(request.FormOrQuery("query")))
            {
                if (_mapServiceInitializer.IsCustomService(id) && service is IDynamicService)
                {
                    layerId = ((IDynamicService)service).GetDynamicQuery(request.FormOrQuery("query"))?.LayerId;
                }
                else
                {
                    var query = await _cache.GetQuery(service.Url, request.FormOrQuery("query"), ui, _urlHelper);
                    if (!String.IsNullOrEmpty(query?.LayerId))
                    {
                        layerId = query.LayerId;
                    }
                }
            }
            string fIds = request.FormOrQuery("fids");

            ILayer layer = service.Layers.FindByLayerId(layerId);
            if (layer == null)
            {
                throw new Exception("Unknown service layer: " + layerId);
            }

            #region VisFilters

            ApplyVisFilters(request, service, ui);

            #endregion

            SelectionCollection selectionCollection = new SelectionCollection(map);

            if (!String.IsNullOrEmpty(layer.IdFieldName))
            {
                var filter = new E.Standard.WebMapping.Core.Filters.QueryFilter(layer.IdFieldName, -1, 0);
                filter.Where = layer.IdFieldName + " in (" + fIds + ")";

                switch (request.FormOrQuery("selection").ToString().ToLower())
                {
                    case "selection":
                        selectionCollection.Add(new Selection(ArgbColor.Cyan, "selection", layer, filter));
                        break;
                    case "query":
                        selectionCollection.Add(new Selection(ArgbColor.Yellow, "query", layer, filter));
                        break;
                    default:
                        selectionCollection.Add(new Selection(ArgbColor.Red, request.FormOrQuery("selection").ToString().ToLower(), layer, filter));
                        break;
                }
            }

            if (selectionCollection.Count > 0)
            {
                response = await service.GetSelectionAsync(selectionCollection, _requestContext);
            }
            else
            {
                response = new EmptyImage(0, String.Empty);
            }
            #endregion
        }

        if (response == null)
        {
            throw new Exception("No response");
        }

        if (response is ImageLocation)
        {
            var imgResponse = (ImageLocation)response;

            ImageLocationResponseDTO img = watch.Apply<ImageLocationResponseDTO>(new ImageLocationResponseDTO()
            {
                id = id,
                url = imgResponse.ImageUrl,
                requestid = requestid,
                scale = imgResponse.Scale,
                extent = imgResponse.Extent != null ?
                    new double[] { imgResponse.Extent.MinX, imgResponse.Extent.MinY, imgResponse.Extent.MaxX, imgResponse.Extent.MaxY } :
                    null
            });
            return img;
        }

        if (response is ErrorResponse)
        {
            ((ErrorResponse)response).ThrowException(
                _urlHelper.AppRootUrl(HttpSchema.Default),
                id,
                "GetSelection",
                requestid,
                true);
        }

        throw new Exception("Unhandeld response type: " + response.GetType());
    }

    async public Task<object> PerformGetLegend(ApiBaseController controller, string id, CmsDocument.UserIdentification ui)
    {
        var request = controller.Request;
        var watch = new StopWatch(id);

        IMap map = _mapServiceInitializer.Map(_requestContext, ui, request.MapName());

        IMapService service = await _cache.GetService(id, map, ui, _urlHelper) ?? await _mapServiceInitializer.GetCustomServiceByUrlAsync(id, map, ui, controller?.Request?.FormCollection());
        if (service == null)
        {
            throw new Exception("Unknown service: " + id);
        }
        var serviceInfo = _restHelper.CreateServiceInfo(controller, service, ui);

        string requestid = request.FormOrQuery("requestid");

        if (service is IMapServiceLegend)
        {
            ((IMapServiceLegend)service).LegendVisible = true;

            map.ImageWidth = int.Parse(request.FormOrQuery("width"));
            map.ImageHeight = int.Parse(request.FormOrQuery("height"));
            string[] bbox = request.FormOrQuery("bbox").ToString().Split(',');
            string[] layerIds = request.FormOrQuery("layers").ToString().Split(',');

            string crsId = request.FormOrQuery("crs");
            if (!String.IsNullOrEmpty(crsId))
            {
                if (crsId.ToLower().StartsWith("epsg:"))
                {
                    crsId = crsId.Split(':')[1];
                }

                map.SpatialReference = E.Standard.Api.App.ApiGlobals.SRefStore.SpatialReferences.ById(int.Parse(crsId));
                if (service is IMapServiceProjection)
                {
                    ((IMapServiceProjection)service).ProjectionMethode = ServiceProjectionMethode.Map;
                    ((IMapServiceProjection)service).RefreshSpatialReference();
                }
            }

            #region Layer Visibility

            foreach (var layer in service.Layers)
            {
                layer.Visible = false;
            }

            foreach (string layerId in layerIds)
            {
                var layer = service.Layers.FindByLayerId(layerId);
                if (layer == null)
                {
                    continue;
                }

                var layerInfo = serviceInfo?.layers?.Where(l => l.id == layer.ID).FirstOrDefault();
                if (layerInfo != null && layerInfo.legend == false)
                {
                    continue;
                }

                if (layer != null)
                {
                    layer.Visible = true;
                }
            }

            #endregion

            map.ZoomTo(new Envelope(
                bbox[0].ToPlatformDouble(),
                bbox[1].ToPlatformDouble(),
                bbox[2].ToPlatformDouble(),
                bbox[3].ToPlatformDouble()
            ));

            if (!String.IsNullOrWhiteSpace(request.FormOrQuery("scale")))
            {
                map.SetScale(request.FormOrQuery("scale").ToPlatformDouble(), map.ImageWidth, map.ImageHeight);
            }

            ServiceResponse response = await ((IMapServiceLegend)service).GetLegendAsync(_requestContext);

            if (response is ImageLocation)
            {
                var imgResponse = (ImageLocation)response;

                ImageLocationResponseDTO img = watch.Apply<ImageLocationResponseDTO>(new ImageLocationResponseDTO()
                {
                    id = id,
                    url = imgResponse.ImageUrl,
                    Path = imgResponse.ImagePath,
                    requestid = requestid
                });
                return img;
            }

            if (response is ErrorResponse)
            {
                ((ErrorResponse)response).ThrowException(
                    _urlHelper.AppRootUrl(HttpSchema.Default),
                    id,
                    "GetLegend",
                    requestid,
                    true);
            }
        }

        return new { requestid = requestid };
    }

    async public Task<object> PerformGetLegendLayerItem(HttpContext httpContext, string id, CmsDocument.UserIdentification ui)
    {
        var request = httpContext.Request;

        var layerId = request.Query["layer"];
        if (String.IsNullOrWhiteSpace(layerId))
        {
            return null;
        }

        string value = request.Query["value"];

        string cacheKey = "layerlegenditem:" + id + ":" + layerId + (String.IsNullOrWhiteSpace(value) ? "" : ":" + value);
        LayerLegendItem item = _cache.Get<LayerLegendItem>(cacheKey);
        if (item != null)
        {
            return item;
        }

        IMap map = _mapServiceInitializer.Map(_requestContext, ui, request.MapName());

        IMapService service = await _cache.GetService(id, map, null, urlHelper: _urlHelper) ?? await _mapServiceInitializer.GetCustomServiceByUrlAsync(id, map, ui, httpContext?.Request?.FormCollection());
        if (service == null)
        {
            throw new Exception("Unknown service: " + id);
        }

        if (service is IMapServiceLegend2)
        {
            var items = await ((IMapServiceLegend2)service).GetLayerLegendItemsAsync(request.Query["layer"], _requestContext);

            if (items != null)
            {
                if (!String.IsNullOrWhiteSpace(value))
                {
                    item = items
                        .Where(m => m.Values != null && m.Values.Contains(value))
                        .FirstOrDefault();
                }

                if (item == null)
                {
                    item = items.Where(m => !m.Label.StartsWith("<")).FirstOrDefault();
                }

                if (item != null)
                {
                    _cache.Set(cacheKey, item);
                    return item;
                }
            }
        }

        return null;
    }

    async public Task<byte[]> PerformTile(HttpContext httpContext, string id, CmsDocument.UserIdentification ui)
    {
        var Request = httpContext.Request;

        //if (Base.IfMatch())
        //{
        //    return Base.NotModified();
        //}

        var service = await _cache.GetService(id, null, ui, _urlHelper) ?? await _mapServiceInitializer.GetCustomServiceByUrlAsync(id, null, ui, httpContext?.Request?.FormCollection());

        TileService tileService = (TileService)service;
        TileGrid tileGrid = tileService.TileGrid;
        Envelope extent = tileGrid.Extent;

        int col = int.Parse(Request.Query["x"]);
        int row = int.Parse(Request.Query["y"]);
        int level = int.Parse(Request.Query["z"]);

        if (tileGrid.Orientation == TileGridOrientation.LowerLeft)
        {
            row = (-row) - 1;
        }

        string tileUrl = tileService.ImageUrl(_requestContext, null);
        tileUrl = tileUrl.Replace("[LEVEL]", level.ToString()).Replace("[COL]", col.ToString()).Replace("[ROW]", row.ToString());

        byte[] data = await _requestContext.Http.GetDataAsync(tileUrl);
        return data;
    }

    public bool TryAddToGraphicsContainer(IMap map, FeatureDTO feature, IGeometricTransformer transformer = null)
    {
        try
        {
            Shape shape = feature.ToShape(), calcShape = null;

            if (transformer != null)
            {
                transformer.Transform(shape);
                shape.SrsId = map.SpatialReference != null ? map.SpatialReference.Id : 0;
            }


            string graphicsTool = String.Empty, graphicsMetaText = String.Empty;
            var meta = feature["_meta"];

            if (JSerializer.IsJsonElement(meta))
            {
                graphicsTool = JSerializer.GetJsonElementValue(meta, "tool").ToStringOrEmpty();
                graphicsMetaText = JSerializer.GetJsonElementValue(meta, "text").ToStringOrEmpty();
            }

            if ((graphicsTool == "dimline"
                || graphicsTool == "dimpolygon"
                || graphicsTool == "hectoline") && feature.GetPropery<int>("_calcCrs") > 0)
            {
                calcShape = feature.ToShape();
                using (var calcTransformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, 4326, feature.GetPropery<int>("_calcCrs")))
                {
                    calcTransformer.Transform(calcShape);
                    calcShape.SrsId = feature.GetPropery<int>("_calcCrs");
                }
            }

            if (shape is E.Standard.WebMapping.Core.Geometry.Point)
            {
                if (graphicsTool == "text")
                {
                    var textColor = feature.GetPropery<string>("font-color").HexToColor(1.0f);
                    map.GraphicsContainer.Add(new LabelElement((E.Standard.WebMapping.Core.Geometry.Point)shape,
                        graphicsMetaText,
                        textColor,
                        (textColor.R + textColor.G + textColor.B) / 3 > 150 ? ArgbColor.Black : ArgbColor.White,
                        E.Standard.Platform.SystemInfo.DefaultFontName,
                        feature.GetPropery<float>("font-size") * SystemInfo.FontSizeFactor));
                }
                else if (graphicsTool == "distance_circle")
                {
                    var dc_steps = Math.Max(1, feature.GetPropery<int>("dc-steps"));
                    map.GraphicsContainer.Add(new DistanceCircleElement((E.Standard.WebMapping.Core.Geometry.Point)shape,
                        feature.GetPropery<double>("dc-radius"),
                        dc_steps,
                        feature.GetPropery<string>("stroke").HexToColor(1.0f),
                        feature.GetPropery<string>("fill").HexToColor(feature.GetPropery<float>("fill-opacity") / dc_steps),
                        feature.GetPropery<float>("stroke-width")));
                }
                else if (graphicsTool == "compass_rose")
                {
                    var cr_steps = Math.Max(1, feature.GetPropery<int>("cr-steps"));
                    map.GraphicsContainer.Add(new CompassRoseElement((E.Standard.WebMapping.Core.Geometry.Point)shape,
                        feature.GetPropery<double>("cr-radius"),
                        cr_steps,
                        feature.GetPropery<string>("stroke").HexToColor(1.0f),
                        feature.GetPropery<float>("stroke-width")));
                }
                else if (graphicsTool == "circle")
                {
                    map.GraphicsContainer.Add(new EllipseElement((E.Standard.WebMapping.Core.Geometry.Point)shape,
                        feature.GetPropery<double>("circle-radius"),
                        feature.GetPropery<double>("circle-radius"),
                        feature.GetPropery<string>("stroke").HexToColor(feature.GetPropery<float>("stroke-opacity")),
                        feature.GetPropery<string>("fill").HexToColor(feature.GetPropery<float>("fill-opacity")),
                        feature.GetPropery<float>("stroke-width"),
                        feature.GetPropery<string>("stroke-style").ToLineStyle()));
                }
                else if (graphicsTool == "point")
                {
                    map.GraphicsContainer.Add(new EllipseElement((Point)shape,
                        feature.GetPropery<double>("point-size"),
                        feature.GetPropery<double>("point-size"),
                        feature.GetPropery<string>("point-color").HexToColor(1),
                        feature.GetPropery<string>("point-color").HexToColor(1),
                        1.0f, String.Empty.ToLineStyle(),
                        EllipseElement.Unit.Pixel
                        ));
                }
                else if (!String.IsNullOrWhiteSpace(feature.GetPropery<string>("symbol")))
                {
                    int hotspotX = -1, hotspotY = -1;
                    FileInfo symbolFile = new FileInfo($"{_urlHelper.WWWRootPath()}/content/api/img/{feature.GetPropery<string>("symbol")}");
                    FileInfo symbolsFile = new FileInfo(symbolFile.Directory.FullName + @"/symbols.xml");
                    if (symbolsFile.Exists)
                    {
                        var symbolsDef = new System.Xml.XmlDocument();
                        symbolsDef.Load(symbolsFile.FullName);

                        var symbolNode = symbolsDef.SelectSingleNode("symbols/symbol[@name='" + symbolFile.Name.ToLower() + "' and @hx and @hy]");
                        if (symbolNode != null)
                        {
                            hotspotX = int.Parse(symbolNode.Attributes["hx"].Value);
                            hotspotY = int.Parse(symbolNode.Attributes["hy"].Value);
                        }
                    }

                    map.GraphicsContainer.Add(new SymbolElement((E.Standard.WebMapping.Core.Geometry.Point)shape,
                        symbolFile.FullName,
                        hotspotX, hotspotY,
                        feature.GetPropery<string>("text")));
                }
            }
            else if (shape is Polyline)
            {
                if (graphicsTool == "dimline")
                {
                    map.GraphicsContainer.Add(new MeasurePolylineElement((Polyline)shape,
                       feature.GetPropery<string>("stroke").HexToColor(),
                       feature.GetPropery<float>("stroke-width"),
                       LineDashStyle.Solid,
                       calcPolyline: calcShape as Polyline,
                       labelTotalLength: feature.GetPropery<bool>("label-total-length"),
                       labelSegments: true,
                       labelPointNumbers: false,
                       fontSize: feature.GetPropery<float>("font-size") * SystemInfo.FontSizeFactor,
                       lengthUnit: feature.GetPropery<string>("length-unit"))
                       );
                }
                else if (graphicsTool == "hectoline")
                {
                    map.GraphicsContainer.Add(new HectoPolylineElement((Polyline)shape,
                       feature.GetPropery<string>("stroke").HexToColor(),
                       feature.GetPropery<float>("stroke-width"),
                       LineDashStyle.Solid,
                       feature.GetPropery<double>("hl-interval"),
                       feature.GetPropery<string>("hl-unit"),
                       calcPolyline: calcShape as Polyline,
                       fontSize: feature.GetPropery<float>("font-size") * SystemInfo.FontSizeFactor));
                }
                else
                {
                    map.GraphicsContainer.Add(new PolylineElement((Polyline)shape,
                        feature.GetPropery<string>("stroke").HexToColor(feature.GetPropery<float>("stroke-opacity")),
                        feature.GetPropery<float>("stroke-width"),
                        feature.GetPropery<string>("stroke-style").ToLineStyle()));
                }
            }
            else if (shape is Polygon)
            {
                if (graphicsTool == "dimpolygon")
                {
                    map.GraphicsContainer.Add(new MeasurePolygonElement((Polygon)shape,
                       feature.GetPropery<string>("stroke").HexToColor(),
                       feature.GetPropery<string>("fill").HexToColor(feature.GetPropery<float>("fill-opacity")),
                       feature.GetPropery<float>("stroke-width"),
                       LineDashStyle.Solid,
                       fontSize: feature.GetPropery<float>("font-size") * SystemInfo.FontSizeFactor,
                       calcPolygon: calcShape as Polygon,
                       labelSegments: feature.GetPropery<bool>("label-edges"),
                       areaUnit: feature.GetPropery<string>("area-unit")));
                }
                else
                {
                    map.GraphicsContainer.Add(new PolygonElement((Polygon)shape,
                        feature.GetPropery<string>("stroke").HexToColor(feature.GetPropery<float>("stroke-opacity")),
                        feature.GetPropery<string>("fill").HexToColor(feature.GetPropery<float>("fill-opacity")),
                        feature.GetPropery<float>("stroke-width"),
                        feature.GetPropery<string>("stroke-style").ToLineStyle()));
                }
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    #endregion

    #region VisFilter, LockedLayer, etc

    public void ApplyVisFilters(HttpRequest httpRequest, IMapService service, CmsDocument.UserIdentification ui)
    {
        var requestVisFilters = httpRequest.VisFilterDefinitionsFromParameters();
        var cmsVisFilters = requestVisFilters?.Where(f => f.IsTocVisFilter() == false).ToArray() ?? [];
        var tocVisFilters = requestVisFilters?.Where(f => f.IsTocVisFilter(service.Url)).ToArray() ?? [];

        var serviceFilters = _cache.GetAllVisFilters(service.Url, ui);

        foreach (var tocVisFilter in tocVisFilters)
        {
            tocVisFilter.CheckSignature(_crypto);

            var layer = service.Layers.FindByLayerId(tocVisFilter.TocVisFilterLayerId());
            if (layer == null) continue;

            layer.Filter = layer.Filter.AppendWhereClause(tocVisFilter.TocVisFilterWhereClause());
        }

        if (cmsVisFilters != null || serviceFilters.HasLockedFilters)
        {
            //var serviceFilters = Cache.GetVisFilters(service.Url, ui);

            if (serviceFilters != null && serviceFilters.filters != null)
            {
                if (cmsVisFilters != null)
                {
                    #region Normal Visfilters

                    foreach (var filter in cmsVisFilters)
                    {
                        if (!String.IsNullOrWhiteSpace(filter.ServiceId) && filter.ServiceId != service.Url)
                        {
                            continue;
                        }

                        var serviceFilter = (from f in serviceFilters.filters where f.Id == filter.Id select f).FirstOrDefault();
                        StringBuilder visFilterClause = new StringBuilder();

                        if (serviceFilter != null)
                        {
                            string filterClause = serviceFilter.Filter;
                            if (filter.Arguments != null)
                            {
                                foreach (var arg in filter.Arguments)
                                {
                                    filterClause = filterClause.Replace("[" + arg.Name + "]", arg.Value);
                                }
                            }

                            if (String.IsNullOrWhiteSpace(filterClause))
                            {
                                continue;
                            }

                            visFilterClause.Append(filterClause);

                            foreach (string layerName in serviceFilter.LayerNamesString.Split(';'))
                            {
                                var layer = service.Layers.Find(l => l.Name == layerName);
                                if (layer == null)
                                {
                                    continue;
                                }

                                //layer.Filter = String.IsNullOrWhiteSpace(layer.Filter) ? visFilterClause.ToString() : $"({ layer.Filter }) AND ({ visFilterClause.ToString() })";
                                layer.Filter = layer.Filter.AppendWhereClause(visFilterClause.ToString());
                            }
                        }
                    }

                    #endregion
                }

                #region Locked Vis Filters

                foreach (var lockedFilter in serviceFilters.LockedFilters)
                {
                    foreach (string layerName in lockedFilter.LayerNamesString.Split(';'))
                    {
                        var layer = service.Layers.Find(l => l.Name == layerName);
                        if (layer == null)
                        {
                            continue;
                        }

                        var lockedFilterClause = CmsHlp.ReplaceFilterKeys(_httpRequestContext?.OriginalUrlParameters, ui, lockedFilter.Filter);

                        layer.Filter = layer.Filter.AppendWhereClause(lockedFilterClause);
                    }
                }

                #endregion
            }
        }
    }

    public void ApplyLockedLayers(IMapService service, IEnumerable<LayerPropertiesDTO> serviceLayerProperites)
    {
        if (serviceLayerProperites != null && serviceLayerProperites.Count() > 0)
        {
            foreach (var layerProperties in serviceLayerProperites)
            {
                if (layerProperties.Locked == true)
                {
                    var layer = service.Layers.Where(l => l.ID == layerProperties.Id).FirstOrDefault();
                    if (layer != null)
                    {
                        layer.Visible = layerProperties.Visible;
                    }
                }
            }
        }
    }

    #endregion
}
