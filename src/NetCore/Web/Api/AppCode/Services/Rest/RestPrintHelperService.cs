using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.DTOs.Print;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.Formatting;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.Core.ServiceResponses;
using E.Standard.WebMapping.GeoServices.Graphics;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicElements;
using E.Standard.WebMapping.GeoServices.Graphics.GraphicsElements;
using E.Standard.WebMapping.GeoServices.Print;
using E.Standard.WebMapping.GeoServices.Tiling;
using gView.GraphicsEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestPrintHelperService
{
    private readonly UrlHelperService _urlHelper;
    private readonly CacheService _cache;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly RestMappingHelperService _restMapping;
    private readonly RestImagingService _restImaging;
    private readonly ConfigurationService _config;
    private readonly BridgeService _bridge;
    private readonly RestToolsHelperService _tools;
    private readonly IRequestContext _requestContext;
    private readonly ICryptoService _crypto;

    public RestPrintHelperService(UrlHelperService urlHelper,
                                  CacheService cache,
                                  MapServiceInitializerService mapServiceInitializer,
                                  RestMappingHelperService restMapping,
                                  RestImagingService restImaging,
                                  ConfigurationService config,
                                  BridgeService bridge,
                                  RestToolsHelperService tools,
                                  IRequestContext requestContext,
                                  ICryptoService crypto)
    {
        _urlHelper = urlHelper;
        _cache = cache;
        _mapServiceInitializer = mapServiceInitializer;
        _restMapping = restMapping;
        _restImaging = restImaging;
        _config = config;
        _bridge = bridge;
        _tools = tools;
        _requestContext = requestContext;
        _crypto = crypto;
    }

    async public Task<IActionResult> PerformPrintAsync(ApiBaseController controller,
                                                       CmsDocument.UserIdentification ui,
                                                       NameValueCollection form = null)
    {
        var httpRequest = controller.Request;
        form = form ?? httpRequest.Form.ToCollection();

        string mapJson = form["map"], graphicsJson = form["graphics"];
        string queryResultFeaturesJson = form["queryResultFeatures"],
               coordinateResultFeaturesJson = form["coordinateResultFeatures"],
               chainageResultFeaturesJson = form["chainageResultFeatures"];

        var mapDefinition = JSerializer.Deserialize<MapDefinitionDTO>(mapJson);
        var graphics = TrySerializeGraphicsFeatures(graphicsJson);

        var showQueryMarkers = "true".Equals(form["showQueryMarkers"], StringComparison.InvariantCultureIgnoreCase);
        var showCoordinateMarkers = "true".Equals(form["showCoordinateMarkers"], StringComparison.InvariantCultureIgnoreCase);
        var showChainageMarkers = "true".Equals(form["showChainageMarkers"], StringComparison.InvariantCultureIgnoreCase);
        var queryFeatures = TrySerializeQueryFeatures(queryResultFeaturesJson);
        var coordinateFeatures = TrySerializeCoordinateFeatures(coordinateResultFeaturesJson);
        var chainageFeatures = TrySerializeChainageFeatures(chainageResultFeaturesJson);
        var queryFeaturesLabelField = form["queryMarkersLabelField"];
        var coordinatesFeaturesLabelField = form["coordinateMarkersLabelField"];
        var chainageFeaturesLabelField = "_fulltext";

        #region Sketch / CalcSketch

        Shape sketch = null, calcSketch = null;
        string sketchLabelMode = String.Empty;
        if (form["_sketchWgs84"] != null)
        {
            sketch = form["_sketchWgs84"].ShapeFromWKT();
            sketchLabelMode = form["_sketchLabels"] ?? String.Empty;

            if (form["_calcSketch"] != null && form["_calcSketchSrs"] != null)
            {
                calcSketch = form["_calcSketch"].ShapeFromWKT();
                SpatialAlgorithms.SetSpatialReferenceAndProjectPoints(calcSketch, int.Parse(form["_calcSketchSrs"]), ApiGlobals.SRefStore.SpatialReferences);
            }
            else
            {
                calcSketch = form["_sketchWgs84"].ShapeFromWKT();
                calcSketch.SrsId = 4326;
            }

            if (form["_calcSrs"] != null)
            {
                var calcCrs = int.Parse(form["_calcSrs"]);
                if (calcCrs > 0 && calcSketch.SrsId > 0 && calcCrs != calcSketch.SrsId)
                {
                    using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, calcSketch.SrsId, calcCrs))
                    {
                        transformer.Transform(calcSketch);
                        calcSketch.SrsId = calcCrs;
                    }
                }
            }
        }

        #endregion

        string layoutId = form["layout"];
        string layoutFormat = form["format"];
        double printScale = !String.IsNullOrWhiteSpace(form["scale"]) ? form["scale"].ToPlatformDouble() : mapDefinition.Scale;
        double printRotation = !String.IsNullOrWhiteSpace(form["rotation"]) ? form["rotation"].ToPlatformDouble() : 0D;
        string layoutDpi = form["dpi"];

        E.Standard.WebMapping.Core.Api.Bridge.IBridge bridge = null;

        #region Layout Settings (show/hide Layers...)

        if (!String.IsNullOrEmpty(layoutId))
        {
            bridge = _bridge.CreateInstance(ui, typeof(E.Standard.WebGIS.Tools.Portal.Publish));

            #region Show layers

            foreach (var serviceLayer in bridge.GetPrintLayoutShowLayers(layoutId))
            {
                var service = mapDefinition.Services.Where(s => s.Id == serviceLayer.serviceId).FirstOrDefault();
                var layer = service?.Layers?.Where(l => l.Id == serviceLayer.layerId).FirstOrDefault();

                if (layer != null)
                {
                    layer.Visible = true;
                }
                else if (service != null) // a locked layer
                {
                    var serviceLayers = new List<MapDefinitionDTO.ServiceDefinition.LayerDefinition>(service.Layers);
                    serviceLayers.Add(new MapDefinitionDTO.ServiceDefinition.LayerDefinitionForceVisibility() { Id = serviceLayer.layerId, Visible = true });
                    service.Layers = serviceLayers.ToArray();
                }
            }

            #endregion

            #region Hide layers

            foreach (var serviceLayer in bridge.GetPrintLayoutHideLayers(layoutId))
            {
                var service = mapDefinition.Services.Where(s => s.Id == serviceLayer.serviceId).FirstOrDefault();
                var layer = service?.Layers?.Where(l => l.Id == serviceLayer.layerId).FirstOrDefault();

                if (layer != null)
                {
                    layer.Visible = false;
                }
            }

            #endregion
        }

        #endregion

        var map = await CreateMap(
                httpRequest, mapDefinition, graphics, ui,
                queryFeatures: showQueryMarkers ? queryFeatures : null,
                queryFeaturesLabelFields: queryFeaturesLabelField,
                coordinateFeatures: showCoordinateMarkers ? coordinateFeatures : null,
                coordinateFeaturesLabelFields: coordinatesFeaturesLabelField,
                chainageFeatures: showChainageMarkers ? chainageFeatures : null,
                chainageFeaturesLabelField: chainageFeaturesLabelField,
                sketch: sketch,
                calcSketch: calcSketch,
                sketchLabelMode: sketchLabelMode
            );

        if (!String.IsNullOrWhiteSpace(layoutId))
        {
            #region Pagesize / Orientation /Dpi

            var pageSize = PageSize.A4;
            var pageOrientation = PageOrientation.Landscape;
            double dpi = 96D;

            if (!String.IsNullOrWhiteSpace(layoutFormat))
            {
                pageSize = layoutFormat.GetPageSize();
                pageOrientation = layoutFormat.GetPageOrientation();
            }

            if (bridge.GetPrintFormats(layoutId)
                      .Where(f => (int)f.Size == (int)pageSize && (int)f.Orientation == (int)pageOrientation)
                      .Count() == 0)
            {
                throw new Exception("pageformat " + layoutFormat + " not supported");
            }

            if (!String.IsNullOrWhiteSpace(layoutDpi))
            {
                dpi = double.Parse(layoutDpi);
            }

            if (dpi > 300)
            {
                throw new Exception("max dpi=300");
            }

            map.Dpi = dpi;
            map.DisplayRotation = printRotation;

            #endregion

            var printLayout = _cache.GetPrintLayouts(_urlHelper.GetCustomGdiScheme(), ui).Where(l => l.Id == layoutId).FirstOrDefault();
            if (printLayout == null)
            {
                throw new Exception("Unkown layout id=" + layoutId);
            }

            LayoutBuilder mainLayoutBuilder = new LayoutBuilder(
                map,
                _requestContext.Http,
                _urlHelper.AppEtcPath() + "/layouts/" + printLayout.LayoutFile,
                pageSize,
                pageOrientation,
                dpi,
                _urlHelper.AppEtcPath() + "/layouts/data");

            List<LayoutBuilder> layoutBuilders = new List<LayoutBuilder>([mainLayoutBuilder]);

            #region SubPages

            foreach (string subpageName in mainLayoutBuilder.SubPages)
            {
                if (String.IsNullOrWhiteSpace(subpageName))
                {
                    continue;
                }

                IMap subMap = map.Clone(null);
                LayoutBuilder subLayoutBuilder = new LayoutBuilder(
                    subMap, _requestContext.Http,
                    _urlHelper.AppEtcPath() + "/layouts/" + subpageName,
                    pageSize,
                    pageOrientation,
                    dpi,
                    _urlHelper.AppEtcPath() + "/layouts/data");

                layoutBuilders.Add(subLayoutBuilder);
            }

            #endregion

            ErrorResponseCollection errorRespones = new ErrorResponseCollection(null);

            Dictionary<LayoutBuilder, string> images = new Dictionary<LayoutBuilder, string>();
            foreach (var layoutBuilder in layoutBuilders)
            {
                #region Layout Text

                foreach (var layoutText in layoutBuilder.UserText ?? new List<LayoutUserText>())
                {
                    if (!String.IsNullOrWhiteSpace(form["LAYOUT_TEXT_" + layoutText.Name]))
                    {
                        layoutText.Value = form["LAYOUT_TEXT_" + layoutText.Name];
                    }
                }

                #endregion

                #region Header ID (from Selected Features)

                if (layoutBuilder.HasHeaderIDQuery)
                {
                    layoutBuilder.HeaderID = form["LAYOUT_HEADER_ID"];
                }

                #endregion

                #region Draw Map

                if (layoutBuilder.Map != null && layoutBuilder.Map.ImageWidth > 0 && layoutBuilder.Map.ImageHeight > 0)
                {
                    layoutBuilder.Map.Dpi = layoutBuilder.PageDpi(dpi);
                    layoutBuilder.Scale = printScale;

                    var mapCenter = new Point(mapDefinition.Center[0], mapDefinition.Center[1]);

                    if (mapDefinition.Crs != null)
                    {
                        var mapSrs = layoutBuilder.PageMapSrs(layoutBuilder.Map.SpatialReference.Id);
                        if (mapSrs != mapDefinition.Crs.Epsg)
                        {
                            map.SpatialReference = ApiGlobals.SRefStore.SpatialReferences.ById(mapSrs);
                            using (var transformer = new GeometricTransformerPro(
                                    ApiGlobals.SRefStore.SpatialReferences,
                                    mapDefinition.Crs.Epsg,
                                    mapSrs))
                            {
                                transformer.Transform(mapCenter);
                            }
                        }
                    }

                    layoutBuilder.Map.SetScale(printScale, layoutBuilder.MapPixels.Width, layoutBuilder.MapPixels.Height, mapCenter.X, mapCenter.Y);

                    layoutBuilder.Map.IsDirty = true;

                    var serviceResponse = await layoutBuilder.Map.GetMapAsync(_requestContext);

                    if (serviceResponse is ImageLocation)
                    {
                        layoutBuilder.MapPath = ((ImageLocation)serviceResponse).ImagePath;
                    }

                    // collect errors
                    if (serviceResponse?.InnerErrorResponse != null)
                    {
                        errorRespones.Add(serviceResponse?.InnerErrorResponse);
                    }

                    #region Koordinatenrahmen

                    layoutBuilder.CoordLeft = layoutBuilder.Map.Extent.LowerLeft.X;
                    layoutBuilder.CoordBottom = layoutBuilder.Map.Extent.LowerLeft.Y;
                    layoutBuilder.CoordRight = layoutBuilder.Map.Extent.UpperRight.X;
                    layoutBuilder.CoordTop = layoutBuilder.Map.Extent.UpperRight.Y;

                    #endregion
                }

                #endregion

                #region Overview Maps

                if (mapDefinition.InitialBounds != null && mapDefinition.InitialBounds.Length == 4)
                {
                    var imageSizeOv = layoutBuilder.OverviewMapPixels;
                    if (imageSizeOv.Width > 0 && imageSizeOv.Height > 0)
                    {
                        var ovMap = _mapServiceInitializer.Map(_requestContext, ui);

                        var ovService = mapDefinition.Services.Where(s => _cache.GetOriginalService(s.Id, ui, _urlHelper).Result is TileService)
                                                              .Select(s => _cache.GetService(s.Id, ovMap, ui, _urlHelper).Result)
                                                              .FirstOrDefault();

                        if (ovService?.Layers != null && ovService.Layers.Count > 0)
                        {
                            ovService.Layers[0].Visible = true;

                            ovMap.Services.Add(ovService);
                            ovMap.ImageWidth = imageSizeOv.Width;
                            ovMap.ImageHeight = imageSizeOv.Height;
                            ovMap.Dpi = map.Dpi;
                            ovMap.SpatialReference = map.SpatialReference;

                            var ovmapBBox = layoutBuilder.GetOvMapBBox();
                            if (ovmapBBox != null)
                            {
                                if (ovMap.SpatialReference != null && ovMap.SpatialReference.Id != 4326)
                                {
                                    using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, 4326, ovMap.SpatialReference.Id))
                                    {
                                        transformer.Transform(ovmapBBox);
                                    }
                                }
                            }
                            else
                            {
                                ovmapBBox = new Envelope(mapDefinition.InitialBounds);
                            }

                            ovMap.ZoomTo(ovmapBBox);

                            var graphicsService = new GraphicsService();
                            await graphicsService.InitAsync(ovMap, _requestContext);
                            ovMap.Services.Add(graphicsService);
                            ovMap.GraphicsContainer.Add(new CrossHairElement(ovMap.Extent, map.Extent, ArgbColor.Red));

                            var ovServiceResponse = await ovMap.GetMapAsync(_requestContext);

                            if (ovServiceResponse is ImageLocation)
                            {
                                layoutBuilder.OverviewMapPath = ((ImageLocation)ovServiceResponse).ImagePath;
                            }
                        }
                    }
                }

                #endregion

                #region Draw Legend

                if (layoutBuilder.Map != null && layoutBuilder.LegendPixels.Width > 0 && layoutBuilder.LegendPixels.Height > 0)
                {
                    layoutBuilder.Map.Dpi = layoutBuilder.PageDpi(dpi);
                    layoutBuilder.Map.SetScale(printScale, 1000, 1000);

                    var serviceResponse = await layoutBuilder.Map.GetLegendAsync(_requestContext);

                    if (serviceResponse is ImageLocation)
                    {
                        layoutBuilder.LegendPath = ((ImageLocation)serviceResponse).ImagePath;
                    }
                }

                #endregion

                #region Overview Windows

                List<OverviewWindow> fsms = layoutBuilder.Overview_Windows;
                List<IMap> fixScaleMaps = new List<IMap>();

                if (fsms != null && fsms.Count > 0)
                {
                    foreach (OverviewWindow fsm in fsms)
                    {
                        IMap fixScaleMap = layoutBuilder.Map.Clone(null);
                        fixScaleMap.ImageWidth = (int)fsm.Size.Width;
                        fixScaleMap.ImageHeight = (int)fsm.Size.Height;
                        fixScaleMap.Dpi = layoutBuilder.DotsPerInch;

                        // 2x Zommen, fallse Zoommaßstab gleich dem Kartenmaßstab -> Bild wird verzerrt!!
                        fixScaleMap.SetScale(fsm.Scale / 2.0, fixScaleMap.ImageWidth, fixScaleMap.ImageHeight, layoutBuilder.Map.Extent.CenterPoint.X, layoutBuilder.Map.Extent.CenterPoint.Y);
                        fixScaleMap.SetScale(fsm.Scale, fixScaleMap.ImageWidth, fixScaleMap.ImageHeight, layoutBuilder.Map.Extent.CenterPoint.X, layoutBuilder.Map.Extent.CenterPoint.Y);

                        #region Darstellungsvariante

                        //if (!String.IsNullOrEmpty(fsm.Presentations))
                        //{
                        //    E.WebMapping.Tools.Presentation presTool = new E.WebMapping.Tools.Presentation();
                        //    MapSession fsSession = new MapSession(mapSession.MapApplication);
                        //    fsSession.Map = fixScaleMap;

                        //    foreach (string pres in fsm.Presentations.Split(','))
                        //    {
                        //        presTool.OnClick(fsSession, new ToolCustomEvent(new string[] { "#" + pres }));
                        //    }
                        //}

                        #endregion

                        fixScaleMaps.Add(fixScaleMap);

                        fixScaleMap.Environment.SetUserValue("OverviewWindows", fsm);
                    }
                }

                foreach (IMap fixedScaleMap in fixScaleMaps)
                {
                    OverviewWindow fsm = (OverviewWindow)fixedScaleMap.Environment.UserValue("OverviewWindows", null);
                    fixedScaleMap.IsDirty = true;
                    ServiceResponse resp = await fixedScaleMap.GetMapAsync(_requestContext);

                    if (resp is ImageLocation)
                    {
                        fsm.ImagePath = ((ImageLocation)resp).ImagePath;
                    }
                }

                #endregion

                string outputPath = map.Environment.UserString(webgisConst.OutputPath) + @"/print_" + Guid.NewGuid().ToString("N").ToLower() + ".png";
                if (await layoutBuilder.Draw(outputPath))
                {
                    images.Add(layoutBuilder, outputPath);
                }
            }

            #region Create Pdf / Zip

            string queryResultFormat = form["attachQueryResults"];
            string coordinatesFormat = form["attachCoordinates"];
            string coordinatesField = form["attachCoordinatesField"];

            string fileName = "print_" + Guid.NewGuid().ToString("N").ToLower(), previewFileName = String.Empty;

            #region Preview

            if (images.Count > 0)
            {
                byte[] previewData = E.Standard.Drawing.Pro.ImageOperations.Scaledown(await images.Values.First().BytesFromUri(_requestContext.Http), 300);
                //System.IO.File.WriteAllBytes(map.Environment.UserString(WebGIS.CMS.webgisConst.OutputPath) + @"/" + (previewFileName = fileName + "_preview.jpg"), previewData);
                await previewData.SaveOrUpload(map.Environment.UserString(webgisConst.OutputPath) + @"/" + (previewFileName = fileName + "_preview.jpg"));
            }

            #endregion

            Dictionary<string, byte[]> outputFileData = new Dictionary<string, byte[]>();

            if (images.Count == 1)
            {
                #region Singlepage Pdf

                var rect = GetPageSize(pageSize, pageOrientation);
                var pic2Pdf = new E.Standard.Plot.Picture2Pdf();

                pic2Pdf.PageWidth = rect.width;
                pic2Pdf.PageHeight = rect.height;
                pic2Pdf.MarginLeft = (float)layoutBuilders[0].BorderLeft;
                pic2Pdf.MarginTop = (float)layoutBuilders[0].BorderTop;
                pic2Pdf.MarginRight = (float)layoutBuilders[0].BorderRight;
                pic2Pdf.MarginBottom = (float)layoutBuilders[0].BorderBottom;

                using (var imageBytes = await images[layoutBuilders[0]].BytesFromUri(_requestContext.Http))
                {
                    var output = pic2Pdf.Convert(_requestContext.Http, imageBytes);

                    outputFileData.Add("map.pdf", output.ToArray());
                }

                #endregion
            }
            else if (images.Count > 1)
            {
                #region Multipage Pdf

                using (var pdf = E.Standard.Plot.MultiPageDocument.CreateMultipageDocument())
                {
                    int pageNumber = 1;
                    foreach (LayoutBuilder layoutBuilder in images.Keys)
                    {
                        var rect = GetPageSize(layoutBuilder.PageSize, layoutBuilder.PageOrientation);

                        if (String.IsNullOrEmpty(images[layoutBuilder]))
                        {
                            continue;
                        }

                        pdf.AddPage(
                                _requestContext.Http, (await images[layoutBuilder].BytesFromUri(_requestContext.Http)).ToArray(),
                                rect.width, rect.height,
                                (int)layoutBuilder.BorderTop, (int)layoutBuilder.BorderBottom, (int)layoutBuilder.BorderLeft, (int)layoutBuilder.BorderRight,
                                $"Seite: {pageNumber++}/{images.Count}"
                             );
                    }

                    var output = pdf.Generate();

                    outputFileData.Add("map.pdf", output.ToArray());
                }

                #endregion
            }

            #endregion

            byte[] outputFileBytes = null;

            #region Attachments

            if (queryFeatures?.Features != null && queryFeatures.Features.Count() > 0)
            {
                string tableFileName = "table.csv";

                var firstFeature = queryFeatures.Features.First();

                if (firstFeature.Oid != null && firstFeature.Oid.Contains(":"))
                {
                    var query = await _cache.GetQuery(firstFeature.Oid.Split(':')[0], firstFeature.Oid.Split(':')[1], ui, urlHelper: _urlHelper);
                    if (query != null)
                    {
                        tableFileName = $"{query.Name.ToValidFilename()}.csv";
                    }
                }

                switch (queryResultFormat)
                {
                    case "csv":
                    case "csv-excel":
                        string csv = queryFeatures.ToCsv(excel: queryResultFormat == "csv-excel");
                        outputFileData.Add(tableFileName, GetCsvEncoding().GetBytes(csv));
                        break;
                }
            }

            if (coordinateFeatures?.Features != null && coordinateFeatures.Features.Count() >= 0 && !String.IsNullOrEmpty(coordinatesField))
            {
                switch (coordinatesFormat)
                {
                    case "csv":
                        var featureCollection = coordinateFeatures.ToFeatureCollecton(coordinatesField);
                        string csv = featureCollection.ToCsv();
                        outputFileData.Add("coordinates.csv", GetCsvEncoding().GetBytes(csv));
                        break;
                }
            }

            #endregion

            if (outputFileData.Count == 1)  // Single (pdf) File
            {
                fileName += ".pdf";

                outputFileBytes = outputFileData.Values.First();
            }
            else  // Zip with Attachments
            {
                var zipStream = new MemoryStream();
                using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (var name in outputFileData.Keys)
                    {
                        var data = outputFileData[name];

                        var entry = zipArchive.CreateEntry(name);
                        using (var entryStream = entry.Open())
                        {
                            entryStream.Write(data, 0, data.Length);
                        }
                    }
                }

                outputFileBytes = zipStream.ToArray();
                fileName += ".zip";
            }

            // Damit kann man die Funktion auf für den PrintServer verwenden...
            if ("base64".Equals(form["result_format"], StringComparison.InvariantCultureIgnoreCase))
            {
                return await controller.JsonObject(new
                {
                    name = fileName,
                    base64 = Convert.ToBase64String(outputFileBytes),
                    page_format = form["format"],
                    scale_dominator = form["scale"].ToPlatformDouble(),
                    success = errorRespones.HasErrors == false,
                    exception = errorRespones.HasErrors ? errorRespones.ErrorMessage : null
                });
            }

            await outputFileBytes.SaveOrUpload($"{map.Environment.UserString(webgisConst.OutputPath)}/{fileName}");

            return await controller.JsonObject(new
            {
                url = map.Environment.UserString(webgisConst.OutputUrl) + "/" + fileName,
                preview = map.Environment.UserString(webgisConst.OutputUrl) + "/" + previewFileName,
                downloadid = _crypto.EncryptTextDefault(fileName, CryptoResultStringType.Hex),
                n = fileName,
                length = outputFileBytes.Length,
                page_format = form["format"],
                scale_dominator = form["scale"].ToPlatformDouble(),
                success = errorRespones.HasErrors == false,
                exception = errorRespones.HasErrors ? errorRespones.ErrorMessage : null
            });
        }
        else
        {
            map.SetScale(printScale,
                         int.Parse(httpRequest.FormOrQuery("imageWidth") ?? "1024"),
                         int.Parse(httpRequest.FormOrQuery("imageHeight") ?? "760"),
                         mapDefinition.Center[0],
                         mapDefinition.Center[1]);

            var mapResponse = await map.GetMapAsync(_requestContext);

            if (mapResponse is ImageLocation)
            {
                return await controller.JsonObject(new
                {
                    url = ((ImageLocation)mapResponse).ImageUrl
                });
            }
        }

        return null;
    }

    async public Task<IActionResult> PerformDownloadMapImageAsync(ApiBaseController controller,
                                                                  CmsDocument.UserIdentification ui)
    {
        var httpRequest = controller.Request;

        var size = httpRequest.Form["size"].ToString()
                                           .Split(",")
                                           .Select(v => int.Parse(v))
                                           .ToArray();
        var displaySize = httpRequest.Form["displaysize"].ToString()
                                                         .Split(",")
                                                         .Select(v => int.Parse(v))
                                                         .ToArray();

        if (size[0] > displaySize[0] || size[1] > displaySize[1])
        {
            throw new Exception($"Maximale Bildgröße von {displaySize[0]}x{displaySize[1]} wurde überschritten. Überprüfen Sie die Anzeige-Eigenschaften des Browsers und stellen Sie gegebenfalls ein Zoomverhältnis größer oder gleich 100% ein.");
        }

        string mapJson = httpRequest.Form["map"], graphicsJson = httpRequest.Form["graphics"];

        var mapDefinition = JSerializer.Deserialize<MapDefinitionDTO>(mapJson);
        var graphics = TrySerializeGraphicsFeatures(graphicsJson);

        #region Bounding Box

        var bboxArray = httpRequest.Form["bbox"].ToString()
                                                .Split(",")
                                                .Select(v => v.ToPlatformDouble())
                                                .ToArray();
        var bbox = new Envelope(bboxArray[0], bboxArray[1], bboxArray[2], bboxArray[3]);

        #endregion

        #region Epsg 

        if (!String.IsNullOrEmpty(httpRequest.Form["image_epsg"]) && int.Parse(httpRequest.Form["image_epsg"]) > 0)
        {
            mapDefinition.Crs.Epsg = int.Parse(httpRequest.Form["image_epsg"]);
            if (!String.IsNullOrEmpty(httpRequest.Form["bbox_epsg"]))
            {
                var bboxEpsg = int.Parse(httpRequest.Form["bbox_epsg"]);
                if (bboxEpsg != mapDefinition.Crs.Epsg)
                {
                    using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, bboxEpsg, mapDefinition.Crs.Epsg))
                    {
                        transformer.Transform(bbox);
                    }
                }
            }
        }
        else if (!String.IsNullOrEmpty(httpRequest.Form["bbox_epsg"]) && mapDefinition.Crs != null) // bbox_espg should be image_epsg => 3D
        {
            var bboxEpsg = int.Parse(httpRequest.Form["bbox_epsg"]);
            if (bboxEpsg > 0 && mapDefinition.Crs.Epsg != bboxEpsg)
            {
                mapDefinition.Crs.Epsg = bboxEpsg;
            }
        }


        #endregion

        var map = await CreateMap(httpRequest, mapDefinition, graphics, ui);

        #region Quality (dpi) 

        int dpi = int.Parse(httpRequest.Form["dpi"]);
        map.Dpi = dpi;

        #endregion

        double dpiFactor = map.Dpi / 96.0;

        #region Map Size

        map.ImageWidth = (int)(size[0] * dpiFactor);
        map.ImageHeight = (int)(size[1] * dpiFactor);

        #endregion

        //#region Epsg 

        //if (!String.IsNullOrEmpty(httpRequest.Form["bbox_epsg"]) && map.SpatialReference != null)
        //{
        //    var bboxEpsg = int.Parse(httpRequest.Form["bbox_epsg"]);
        //    if (bboxEpsg > 0 && bboxEpsg != map.SpatialReference.Id)
        //    {
        //        SpatialReference sRef = Globals.SpatialReferences.ById(bboxEpsg);
        //        map.SpatialReference = sRef;

        //        //Console.WriteLine($"DownloadImage - SpatialReference: { sRef.Id } - { sRef?.Proj4 }");
        //    }
        //}

        //#endregion

        #region Zoom To

        map.ZoomTo(bbox);

        #endregion

        #region ImageFormat

        string format = httpRequest.Form["format"].ToString().ToLower().OrTake("jpg");

        #endregion

        var imageResponse = await map.GetMapAsync(_requestContext, format: format) as ImageLocation;
        if (String.IsNullOrEmpty(imageResponse?.ImagePath))
        {
            throw new Exception("Internal error: can't crate map image");
        }

        if (httpRequest.Form["worldfile"].ToString().ToLower() == "true")
        {
            #region WorldFile

            string worldFilename = $"{imageResponse.ImagePath.Substring(0, imageResponse.ImagePath.LastIndexOf("."))}.{WorldFileExtension(format)}";
            double pix = map.MapScale / (map.Dpi / 0.0254);

            var origin = map.ImageToWorld(new E.Standard.WebMapping.Core.Geometry.Point(0, 0));

            using (StreamWriter sr = new StreamWriter(worldFilename))
            {
                double r = 0D, pixFactor = 1.0;

                if (map.SpatialReference.IsWebMercator())
                {
                    using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, map.SpatialReference.Id, 4326))
                    {
                        var centerPoint = new Point(map.Extent.CenterPoint);
                        transformer.Transform(centerPoint);

                        pixFactor = 1.0 / Math.Cos(centerPoint.Y * Math.PI / 180.0);
                    }
                }

                sr.WriteLine((pix * Math.Cos(r) * pixFactor).ToPlatformNumberString());
                sr.WriteLine((pix * -Math.Sin(r) * pixFactor).ToPlatformNumberString());
                sr.WriteLine((-pix * Math.Sin(r) * pixFactor).ToPlatformNumberString());
                sr.WriteLine((-pix * Math.Cos(r) * pixFactor).ToPlatformNumberString());

                sr.WriteLine(origin.X.ToPlatformNumberString());
                sr.WriteLine(origin.Y.ToPlatformNumberString());
            }

            #endregion

            string zipName = $"{imageResponse.ImagePath.Substring(0, imageResponse.ImagePath.LastIndexOf("."))}.zip";

            #region Zip

            var memStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(memStream, ZipArchiveMode.Create, true))
            {
                zipArchive.CreateEntryFromFile(imageResponse.ImagePath, $"mapimage.{format}");
                zipArchive.CreateEntryFromFile(worldFilename, $"mapimage.{WorldFileExtension(format)}");

                #region Prj File

                if (map.SpatialReference != null)
                {
                    var configPrjFileinfo = new FileInfo(System.IO.Path.Combine(ApiGlobals.AppEtcPath, "prj", $"{map.SpatialReference.Id}.prj"));
                    if (configPrjFileinfo.Exists)
                    {
                        zipArchive.CreateEntryFromFile(configPrjFileinfo.FullName, $"mapimage.prj");
                    }
                }

                #endregion
            }

            File.WriteAllBytes(zipName, memStream.ToArray());

            #endregion

            #region Cleanup

            try
            {
                File.Delete(imageResponse.ImagePath);
                File.Delete(worldFilename);
            }
            catch { }

            #endregion

            return await PrintResponse(controller, "mapimage.zip", zipName);
        }
        else
        {
            return await PrintResponse(controller, $"mapimage.{format}", imageResponse.ImagePath);
        }
    }


    // Test: Postman
    // https://localhost:44341/rest/plotservice
    // mapName=Basiskarte&mapCategory=Allgemein&mapPortal=dev&layout=lkmyzkjugleodsjpu-75pdg@ccgis_default&dpi=150&scale=1000000&bbox=-68310.17,215052.83,-67577.19,216004.68&bbox_srs=31256
    // mapName=Basiskarte&mapCategory=Allgemein&mapPortal=dev&layout=strom-standard&dpi=150&scale=10000&bbox=-68310.17,215052.83,-67577.19,216004.68&bbox_srs=31256
    async public Task<IActionResult> PerformPlotServiceRequestAsync(ApiBaseController controller,
                                                                    CmsDocument.UserIdentification ui)
    {
        var httpRequest = controller.Request;
        var form = httpRequest.Form.ToCollection();

        string mapName = form["mapName"],
               mapCategory = form["mapCategory"],
               mapPortal = form["mapPortal"];

        #region load map from stroage

        var bridge = _bridge.CreateInstance(ui, typeof(E.Standard.WebGIS.Tools.Portal.Publish));
        var nvc = new NameValueCollection();
        nvc["page"] = mapPortal;
        nvc["category"] = mapCategory;
        nvc["map"] = mapName;
        var e = new ApiToolEventArguments(bridge, nvc);

        var toolResponse = await _tools.InvokeServerCommandAsync<ApiEventResponse, E.Standard.WebGIS.Tools.Portal.Publish>("mapjson", bridge, e) as MapJsonResponse;

        if (String.IsNullOrEmpty(toolResponse?.SerializationMapJson))
        {
            throw new Exception($"Can't load map: {mapPortal}/{mapCategory}/{mapName}");
        }

        var mapDefinition = JSerializer.Deserialize<MapDefinitionUiDTO>(toolResponse.SerializationMapJson);

        var map = await CreateMap(httpRequest, mapDefinition, null, ui);

        #endregion

        #region Bounding Box / CenterPoint

        var bbox = form["bbox"].ToString()
                                          .Split(",")
                                          .Select(v => v.ToPlatformDouble())
                                          .ToArray();
        var bboxEnvelope = new Envelope(bbox[0], bbox[1], bbox[2], bbox[3]);
        int bboxSrs = int.Parse(form["bbox_srs"]?.ToString().OrTake(map.SpatialReference.Id.ToString()));

        if (bboxSrs != map.SpatialReference.Id)
        {
            using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences.ById(bboxSrs), map.SpatialReference))
            {
                transformer.Transform(bboxEnvelope);
            }
        }

        using (var transformer = new GeometricTransformerPro(map.SpatialReference, ApiGlobals.SRefStore.SpatialReferences.ById(4326)))
        {
            // after CreateMap => project it back to 4236. It will projected again in PerformPrintAsync.CreateMap

            var centerPoint = bboxEnvelope.CenterPoint;
            transformer.Transform(centerPoint);

            mapDefinition.Center = [centerPoint.X, centerPoint.Y];

            if (mapDefinition.Bounds != null)
            {
                var bounds = new Envelope(mapDefinition.Bounds);
                transformer.Transform(bounds);
                mapDefinition.Bounds = [bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY];
            }

            if (mapDefinition.InitialBounds != null)
            {
                var initialBounds = new Envelope(mapDefinition.InitialBounds);
                transformer.Transform(initialBounds);
                mapDefinition.InitialBounds = [initialBounds.MinX, initialBounds.MinY, initialBounds.MaxX, initialBounds.MaxY];
            }
        }

        #endregion

        #region Print Layout

        var layoutId = form["layout"];
        var printLayout = _cache.GetPrintLayouts(_urlHelper.GetCustomGdiScheme(), ui).Where(l => l.Id == layoutId)
                                .FirstOrDefault() ??
                          _cache.GetPrintLayouts(_urlHelper.GetCustomGdiScheme(), ui).Where(l => l.Name.Equals(layoutId, StringComparison.InvariantCultureIgnoreCase))
                                .FirstOrDefault();

        if (printLayout == null)
        {
            throw new Exception($"Unknown print layout: {layoutId}");
        }

        if (!mapDefinition.HasPrintLayout(printLayout.Id))
        {
            throw new Exception($"Print layout: {printLayout.Name} not in included in map {mapPortal}/{mapCategory}/{mapName}");
        }

        form["layout"] = printLayout.Id;

        #endregion

        #region DPI

        double dpi = (double)form["dpi"]?.ToString().OrTake("120").ToPlatformDouble();
        map.Dpi = dpi;
        form["dpi"] = ((int)dpi).ToString();

        #endregion

        #region Best Format/Pagesize for scale

        var layoutBuilder = new LayoutBuilder(map, _requestContext.Http, _urlHelper.AppEtcPath() + "/layouts/" + printLayout.LayoutFile, PageSize.A4, PageOrientation.Portrait, dpi, _urlHelper.AppEtcPath() + "/layouts/data");

        if (layoutBuilder.AllowedWithPlotService() == false)
        {
            throw new Exception($"Print Layout is not allowed with plot-service. Set 'plot_service_page_sizes' and 'plot_service_page_orientations' attributes with <layout> XML tag.");
        }

        var targetScales = form["scale"].ToPlattformDoubleArray();

        if (layoutBuilder.GetAllowedScales() != null)
        {
            foreach (var targetScale in targetScales)
            {
                if (!layoutBuilder.GetAllowedScales().Contains((int)targetScale))
                {
                    throw new Exception($"Scale 1:{targetScale} is not allowed with print layout {printLayout.Name}. Allowed print scale dominators defined in layout: {String.Join(",", layoutBuilder.GetAllowedScales())}");
                }
            }
        }

        bool foundFittingPageSize = false;
        StringBuilder scalesString = new StringBuilder();

        foreach (var targetScale in targetScales)
        {
            foreach (var format in layoutBuilder.GetPlotServicePageFormats())
            {
                layoutBuilder.PageSize = format.size;
                layoutBuilder.PageOrientation = format.orientation;

                map.ImageWidth = layoutBuilder.MapPixels.Width;
                map.ImageHeight = layoutBuilder.MapPixels.Height;

                map.ZoomTo(bboxEnvelope);

                scalesString.Append($"{format.size.ToString()}.{format.orientation.ToString()} => {Math.Round(map.MapScale, 1)},");

                if (map.MapScale <= targetScale)
                {
                    form["format"] = $"{format.size}.{format.orientation}";
                    form["scale"] = targetScale.ToString();

                    foundFittingPageSize = true;
                    break;
                }
            }

            if (foundFittingPageSize)
            {
                break;
            }
        }

        if (!foundFittingPageSize)
        {
            throw new Exception($"No fitting page size/format found for targetscales [{String.Join(",", targetScales)}]: . Scales: {scalesString}");
        }

        #endregion

        #region Request Parameters (alles was nicht beim Drucken sowieso noch passiert, also beispielsweise Darstellungsfilter hier nicht behandeln)

        #region Darstellungsvariatenten und Sichbarkeit

        var presentationsJson = form["presentations"];
        if (!String.IsNullOrEmpty(presentationsJson))
        {
            var presentationDefinintions = JSerializer.Deserialize<IEnumerable<PresentationDefintionDTO>>(presentationsJson);
            foreach (var presentationDefintion in presentationDefinintions)
            {
                var service = mapDefinition.Services
                                           .Where(s => s.Id == presentationDefintion.ServiceId)
                                           .FirstOrDefault();

                if (service == null)
                {
                    throw new Exception($"Apply presentations: Unknown service {presentationDefintion.ServiceId}");
                }

                if (service.Layers == null)
                {
                    continue;
                }

                var presentation = _cache.GetPresentations(await _cache.GetOriginalService(presentationDefintion.ServiceId, ui, _urlHelper), _urlHelper.GetCustomGdiScheme(), ui)
                      .presentations
                      .Where(p => p.id == presentationDefintion.Id)
                      .FirstOrDefault();

                if (presentation == null)
                {
                    throw new Exception($"Apply presentations: Unknown presentation {presentationDefintion.Id} in service {presentationDefintion.ServiceId}");
                }

                if (presentationDefintion.Check.HasValue == false)
                {
                    // Button => alle Layer für diesen Dienst ausschalten
                    foreach (var layer in service.Layers)
                    {
                        layer.Visible = false;
                    }
                }

                if (presentation.layers != null)
                {
                    foreach (var layerName in presentation.layers)
                    {
                        if (!String.IsNullOrEmpty(layerName))
                        {
                            var layer = service.Layers.Where(l => layerName.Equals(l.Name)).FirstOrDefault();
                            if (layer == null)
                            {
                                throw new Exception($"Apply presentations: {presentation.name} ({presentation.id}) includes unknown service layer name {layerName}");
                            }

                            layer.Visible = presentationDefintion.Check.HasValue == false || presentationDefintion.Check.Value == true;
                        }
                    }
                }
            }
        }

        var layersJson = form["layers"];
        if (!String.IsNullOrEmpty(layersJson))
        {
            var layerDefintions = JSerializer.Deserialize<IEnumerable<LayerVisibilityDefinitionDTO>>(layersJson);

            var services = mapDefinition.Services
                                        .Select(async s => await _cache.GetService(s.Id, null, ui, _urlHelper))
                                        .Select(t => t.Result)
                                        .ToArray();

            foreach (var layerDefintion in layerDefintions)
            {
                if (layerDefintion?.Layers == null)
                {
                    continue;
                }

                var iService = services.Where(s => s.Url == layerDefintion.ServiceId)
                                       .FirstOrDefault();

                #region Basemap => alle anderen Basemaps ausschalten

                if (iService.IsBaseMap && iService.BasemapType == BasemapType.Normal)
                {
                    foreach (var iBasemapService in services.Where(s => s.IsBaseMap && iService.BasemapType == BasemapType.Normal))
                    {
                        var basemapService = mapDefinition.Services.Where(s => s.Id == iBasemapService.Url).FirstOrDefault();
                        if (basemapService?.Layers != null)
                        {
                            foreach (var basemapLayer in basemapService.Layers)
                            {
                                basemapLayer.Visible = false;
                            }
                        }
                    }
                }

                #endregion

                var service = mapDefinition.Services
                                           .Where(s => s.Id == layerDefintion.ServiceId)
                                           .FirstOrDefault();

                if (service == null)
                {
                    throw new Exception($"Apply layer visibility: Unknown service {layerDefintion.ServiceId}");
                }

                foreach (var layerId in layerDefintion.Layers.Where(l => l != null))
                {
                    var layer = service.Layers?.Where(l => l.Id == layerId).FirstOrDefault();
                    if (layer == null)
                    {
                        layer = service.Layers?.Where(l => layerId.Equals(l.Name)).FirstOrDefault();
                    }

                    if (layer == null)
                    {
                        throw new Exception($"Apply layer visibility: Unknown layer (id/name) {layerId} in service {layerDefintion.ServiceId}");
                    }

                    layer.Visible = layerDefintion.Visible;
                }
            }
        }

        #endregion

        #endregion

        #region remove forbidden print parameters

        foreach (string forbiddenParameter in new string[] {
            "rotation",
            "showQueryMarkers",
            "showCoordinateMarkers",
            "queryMarkersLabelField",
            "coordinateMarkersLabelField",
            "queryResultFeatures",
            "coordinateResultFeatures",
            "attachQueryResults",
            "attachCoordinates",
            "attachCoordinatesField" })
        {
            form[forbiddenParameter] = null;
        }

        #endregion

        form["map"] = JSerializer.Serialize(mapDefinition);
        form["result_format"] = form["result_format"].OrTake("base64");

        return await PerformPrintAsync(controller, ui, form);
    }

    #region Helper

    async private Task<IMap> CreateMap(HttpRequest httpRequest,
                                       MapDefinitionDTO mapDefinition,
                                       FeaturesDTO graphics,
                                       CmsDocument.UserIdentification ui,
                                       QueryFeaturesDTO queryFeatures = null,
                                       string queryFeaturesLabelFields = null,
                                       CoordinateFeaturesDTO coordinateFeatures = null,
                                       string coordinateFeaturesLabelFields = null,
                                       ChainageFeaturesDTO chainageFeatures = null,
                                       string chainageFeaturesLabelField = null,
                                       Shape sketch = null, Shape calcSketch = null,
                                       string sketchLabelMode = "")
    {
        if (mapDefinition == null)
        {
            throw new ArgumentException("mapDefintion == null");
        }

        var map = _mapServiceInitializer.Map(_requestContext, ui);

        var graphicFeatuers = new List<E.Standard.Api.App.DTOs.FeatureDTO>();
        if (graphics?.features != null)
        {
            graphicFeatuers.AddRange(graphics.features);
        }

        var labelingDefinitions = httpRequest.LabelDefinitionsFromParameters();

        using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, 4326, mapDefinition.Crs != null && mapDefinition.Crs.Epsg != 4326 ? mapDefinition.Crs.Epsg : 4326))
        {
            map.SpatialReference = ApiGlobals.SRefStore.SpatialReferences.ById(mapDefinition.Crs.Epsg);

            #region Center Point

            if (mapDefinition.Crs != null && mapDefinition.Crs.Epsg != 4326)
            {
                var centerPoint = new Point(mapDefinition.Center[0], mapDefinition.Center[1]);
                transformer.Transform(centerPoint);
                mapDefinition.Center[0] = centerPoint.X;
                mapDefinition.Center[1] = centerPoint.Y;
            }

            #endregion

            #region Bounds

            if (mapDefinition.Crs != null && mapDefinition.Crs.Epsg != 4326 && mapDefinition.Bounds != null && mapDefinition.Bounds.Length == 4)
            {
                Point lowerLeft = new Point(mapDefinition.Bounds[0], mapDefinition.Bounds[1]),
                      upperRight = new Point(mapDefinition.Bounds[2], mapDefinition.Bounds[3]);

                transformer.Transform(lowerLeft);
                transformer.Transform(upperRight);
                mapDefinition.Bounds[0] = lowerLeft.X;
                mapDefinition.Bounds[1] = lowerLeft.Y;
                mapDefinition.Bounds[2] = upperRight.X;
                mapDefinition.Bounds[3] = upperRight.Y;
            }

            #endregion

            #region InitialBounds (Full Extent)

            if (mapDefinition.Crs != null && mapDefinition.Crs.Epsg != 4326 && mapDefinition.InitialBounds != null && mapDefinition.InitialBounds.Length == 4)
            {
                Point lowerLeft = new Point(mapDefinition.InitialBounds[0], mapDefinition.InitialBounds[1]),
                      upperRight = new Point(mapDefinition.InitialBounds[2], mapDefinition.InitialBounds[3]);

                transformer.Transform(lowerLeft);
                transformer.Transform(upperRight);
                mapDefinition.InitialBounds[0] = lowerLeft.X;
                mapDefinition.InitialBounds[1] = lowerLeft.Y;
                mapDefinition.InitialBounds[2] = upperRight.X;
                mapDefinition.InitialBounds[3] = upperRight.Y;
            }

            #endregion

            #region Services

            var serviceDefinitions = mapDefinition.Services;

            #region Service Order

            // Alle Dienste müssen ein Order Flag haben
            if (serviceDefinitions.Where(s => !s.Order.HasValue).Count() == 0)
            {
                serviceDefinitions = serviceDefinitions
                                        .OrderBy(s => s.Order.Value)
                                        .ToArray();
            }

            #endregion

            foreach (var serviceDefintion in serviceDefinitions)
            {
                var getServiceResult = await _cache.GetServiceAndLayerProperties(serviceDefintion.Id, map, ui, _urlHelper);
                if (getServiceResult.service == null)
                {
                    getServiceResult = (service: await _mapServiceInitializer.GetCustomServiceByUrlAsync(serviceDefintion.Id, map, ui, httpRequest.FormCollection()), serviceLayerProperties: new LayerPropertiesDTO[0]);
                }

                var service = getServiceResult.service ?? await _bridge.TryCreateCustomToolService(ui, map, serviceDefintion.Id);
                var serviceLayerProperites = getServiceResult.serviceLayerProperties;

                if (service == null)
                {
                    throw new Exception("Unkonwn Service: " + serviceDefintion.Id);
                }

                if (mapDefinition.FocusedServices?.Ids != null && mapDefinition.FocusedServices.Ids.Length > 0)
                {
                    service.InitialOpacity = mapDefinition.FocusedServices.Ids.Contains(service.Url)
                        ? 1.0f
                        : mapDefinition.FocusedServices.Opacity;
                }
                else
                {
                    service.InitialOpacity = serviceDefintion.Opacity;
                }

                if (service is IServiceLegend)
                {
                    ((IServiceLegend)service).LegendVisible = true;
                }

                #region Spatial References (always use map)

                if (service is IServiceProjection)
                {
                    ((IServiceProjection)service).ProjectionMethode = ServiceProjectionMethode.Map;
                    ((IServiceProjection)service).RefreshSpatialReference();
                }

                #endregion

                #region Turn off all layers

                foreach (var layer in service.Layers)
                {
                    layer.Visible = false;
                }

                #endregion

                #region Layer visibility from serviceDefintion

                foreach (var layerDefinition in serviceDefintion.Layers)
                {
                    var layer = service.Layers.Where(l => l.ID == layerDefinition.Id).FirstOrDefault();
                    if (layer == null || layer.Name != layerDefinition.Name)  // Ids können sich ändern, wenn name nicht passt sollte besse name genommen werden
                    {
                        layer = layer ?? service.Layers.Where(l => l.Name == layerDefinition.Name).FirstOrDefault();   // Wenn Name gar nicht existiert -> bleibt Ids
                    }
                    if (layer != null)
                    {
                        layer.Visible = layerDefinition.Visible;
                    }
                }

                #endregion

                #region Locked Layers

                _restMapping.ApplyLockedLayers(service, serviceLayerProperites);

                #endregion

                #region Force Visibility Layers (show_layers in layout.xml overrides locked layers!) 

                foreach (var layerDefinition in serviceDefintion.Layers.Where(l => l is MapDefinitionDTO.ServiceDefinition.LayerDefinitionForceVisibility))
                {
                    var layer = service.Layers.Where(l => l.ID == layerDefinition.Id).FirstOrDefault();
                    if (layer != null)
                    {
                        layer.Visible = layerDefinition.Visible;
                    }
                }

                #endregion

                #region VisFilter

                _restMapping.ApplyVisFilters(httpRequest, service, ui);

                #endregion

                #region Labeling

                service.AddLabeling(labelingDefinitions, _cache, ui);

                #endregion

                #region Increase Timeout, when printing, because bigger plans need often more time!

                service.Timeout = Math.Max(service.Timeout, 120);  // max 2 minutes (empirical)

                #endregion

                map.Services.Add(service);
            }

            #endregion

            #region Selection

            if (mapDefinition.Selections != null && mapDefinition.Selections.Count() > 0)
            {
                foreach (var selectionDefinition in mapDefinition.Selections)
                {
                    if (selectionDefinition.Type == "custom")
                    {
                        #region Custom Selection (from json file, eg buffer)

                        FileInfo fi = new FileInfo($"{_urlHelper.OutputPath()}/{selectionDefinition.CustomId}.json");
                        if (fi.Exists)
                        {
                            var geoJsonFeatures = JSerializer.Deserialize<FeaturesDTO>(await File.ReadAllTextAsync(fi.FullName));
                            if (geoJsonFeatures?.features != null)
                            {
                                graphicFeatuers.AddRange(geoJsonFeatures.features);
                            }
                        }

                        #endregion
                    }
                    else
                    {
                        #region Service Selection

                        var selectionService = map.Services.FindByUrl(selectionDefinition.ServiceId);
                        if (selectionService == null)
                        {
                            throw new Exception("unkown service '" + selectionDefinition.ServiceId + "'");
                        }

                        QueryDTO query = null;
                        if (_mapServiceInitializer.IsCustomService(selectionDefinition.ServiceId))
                        {
                            var service = await _mapServiceInitializer.GetCustomServiceByUrlAsync(selectionDefinition.ServiceId, _mapServiceInitializer.Map(_requestContext, ui), ui, httpRequest?.FormCollection());
                            if (service is IDynamicService && ((IDynamicService)service).CreateQueriesDynamic != ServiceDynamicQueries.Manually)
                            {
                                query = ((IDynamicService)service).GetDynamicQuery(selectionDefinition.QueryId);
                            }
                        }
                        else
                        {
                            query = await _cache.GetQuery(selectionDefinition.ServiceId, selectionDefinition.QueryId, ui, urlHelper: _urlHelper);
                        }
                        if (query == null)
                        {
                            throw new Exception("Unknow query '" + selectionDefinition.QueryId + "' in service '" + selectionDefinition.ServiceId + "'");
                        }

                        string layerId = query.LayerId;
                        var layer = selectionService.Layers.FindByLayerId(layerId);
                        if (layer == null)
                        {
                            throw new Exception("Unknown selection layer '" + layerId + "' in service '" + selectionDefinition.ServiceId + "'");
                        }

                        var filter = new QueryFilter(layer.IdFieldName, -1, 0);
                        filter.Where = layer.IdFieldName + " in (" + selectionDefinition.FeatureIds + ")";

                        var color = ArgbColor.Yellow;
                        switch (selectionDefinition.Type)
                        {
                            case "selection":
                                color = ArgbColor.Cyan;
                                break;
                        }

                        Selection selection = new Selection(color, selectionDefinition.Type, layer, filter);

                        map.Selection.Add(selection);

                        #endregion
                    }
                }
            }

            #endregion

            #region Graphics

            foreach (var feature in graphicFeatuers)
            {
                _restMapping.TryAddToGraphicsContainer(map, feature, transformer);
            }

            #region Add Coordinate Markers

            if (coordinateFeatures?.Features != null)
            {
                foreach (var coordianteFeature in coordinateFeatures.Features)
                {
                    var point = new E.Standard.WebMapping.Core.Geometry.Point(coordianteFeature.Geometry.coordinates[0], coordianteFeature.Geometry.coordinates[1]);
                    if (transformer != null)
                    {
                        transformer.Transform(point);
                    }

                    map.GraphicsContainer.Add(new ImageElement(
                        _restImaging.GetCoordsMarkerImageBytes(coordianteFeature.MarkerIndex),
                        point,
                        _restImaging.GetCoordsMarkerImageOffset()));

                    if (!String.IsNullOrWhiteSpace(coordinateFeaturesLabelFields))
                    {
                        try
                        {
                            List<string> labels = new List<string>();
                            bool showMulitple = coordinateFeaturesLabelFields.Contains(';');
                            foreach (var coordinateFeaturesLabelField in coordinateFeaturesLabelFields.Split(';'))
                            {
                                var coordinateLabels = JSerializer.Deserialize<string[]>(coordianteFeature.Properties[coordinateFeaturesLabelField]?.ToString() ?? String.Empty)
                                                                  .Where(l => !String.IsNullOrEmpty(l)).ToList();

                                if (showMulitple)
                                {
                                    coordinateLabels.Insert(0, $"{coordinateFeaturesLabelField}:");
                                    coordinateLabels = new List<string>([String.Join(" ", coordinateLabels)]);
                                }

                                labels.AddRange(coordinateLabels);
                            }
                            map.GraphicsContainer.Add(new BlockoutLabelElement(
                                    point,
                                    String.Join('\n', labels),
                                    ArgbColor.Black,
                                    ArgbColor.White,
                                    E.Standard.Platform.SystemInfo.DefaultFontName,
                                    10,
                                    new Offset(12, 0)));
                        }
                        catch { }
                    }
                }
            }

            #endregion

            #region Add Chainage Markers

            if (chainageFeatures?.Features != null)
            {
                foreach (var chainageFeature in chainageFeatures.Features)
                {
                    var point = new E.Standard.WebMapping.Core.Geometry.Point(chainageFeature.Geometry.coordinates[0], chainageFeature.Geometry.coordinates[1]);
                    if (transformer != null)
                    {
                        transformer.Transform(point);
                    }

                    map.GraphicsContainer.Add(new ImageElement(
                       _restImaging.GetChainageMarkerImageBytes(null),
                       point,
                       _restImaging.GetChainageMarkerImageOffset()));

                    if (!String.IsNullOrWhiteSpace(chainageFeaturesLabelField))
                    {
                        try
                        {
                            var label = chainageFeature.GetAttributeLabel(chainageFeaturesLabelField, LabelUnionMode.DistinctCounter)
                                                       .StripHTMLFromLabel();

                            map.GraphicsContainer.Add(new BlockoutLabelElement(
                                point,
                                label,
                                ArgbColor.Black,
                                ArgbColor.White,
                                E.Standard.Platform.SystemInfo.DefaultFontName,
                                10,
                                new Offset(12, 0)));
                        }
                        catch { }
                    }
                }
            }

            #endregion

            #region Add Query Markers

            if (queryFeatures?.Features != null)
            {
                foreach (var queryFeature in queryFeatures.Features)
                {
                    var point = new E.Standard.WebMapping.Core.Geometry.Point(queryFeature.Geometry.coordinates[0], queryFeature.Geometry.coordinates[1]);
                    if (transformer != null)
                    {
                        transformer.Transform(point);
                    }

                    map.GraphicsContainer.Add(new ImageElement(
                        _restImaging.GetQueryMarkerImageBytes(queryFeature.MarkerIndex + 1),
                        point,
                        _restImaging.GetQueryMarkerImageOffset()));

                    if (!String.IsNullOrWhiteSpace(queryFeaturesLabelFields))
                    {
                        try
                        {
                            List<string> labels = new List<string>();
                            bool showMulitple = queryFeaturesLabelFields.Contains(';');
                            foreach (var queryFeaturesLabelField in queryFeaturesLabelFields.Split(';'))
                            {
                                var label = queryFeature.GetAttributeLabel(queryFeaturesLabelField, LabelUnionMode.DistinctCounter)
                                                        .StripHTMLFromLabel();

                                if (String.IsNullOrEmpty(label))
                                {
                                    continue;
                                }
                                if (showMulitple)
                                {
                                    label = $"{queryFeaturesLabelField}: {label}";
                                }

                                labels.Add(label);
                            }

                            map.GraphicsContainer.Add(new BlockoutLabelElement(
                                point,
                                String.Join('\n', labels),
                                ArgbColor.Black,
                                ArgbColor.White,
                                SystemInfo.DefaultFontName,
                                10,
                                new Offset(12, -32)));
                        }
                        catch { }

                    }
                }
            }
            #endregion

            #region Add Sketch

            if (sketch != null)
            {
                if (transformer != null)
                {
                    transformer.Transform(sketch);
                }
                if (sketch is Polygon)
                {
                    map.GraphicsContainer.Add(new MeasurePolygonElement((Polygon)sketch, ArgbColor.Red, ArgbColor.FromArgb(120, ArgbColor.Yellow), 3,
                        calcPolygon: calcSketch as Polygon,
                        labelSegments: sketchLabelMode == "segments"));
                }
                else if (sketch is Polyline)
                {
                    map.GraphicsContainer.Add(new MeasurePolylineElement((Polyline)sketch, ArgbColor.Red, 3,
                        calcPolyline: calcSketch as Polyline,
                        labelSegments: sketchLabelMode == "segments", labelPointNumbers: sketchLabelMode == "segments"));
                }
            }

            #endregion

            if (map.GraphicsContainer.Count() > 0)
            {
                var graphicsService = new GraphicsService();
                await graphicsService.InitAsync(map, _requestContext);
                map.Services.Add(graphicsService);
            }

            #endregion
        }

        return map;
    }

    private FeaturesDTO TrySerializeGraphicsFeatures(string graphicsJson)
    {
        try
        {
            // es kann zu fehlern kommen, wenn ungültige geometrien daherkommen, zB Punkte mit coordinates [null, null], ...
            return !String.IsNullOrWhiteSpace(graphicsJson) ? JSerializer.Deserialize<FeaturesDTO>(graphicsJson) : null;
        }
        catch
        {
            return null;
        }
    }

    private QueryFeaturesDTO TrySerializeQueryFeatures(string queryResultFeaturesJson)
    {
        try
        {
            return !String.IsNullOrWhiteSpace(queryResultFeaturesJson) ?
                JSerializer.Deserialize<QueryFeaturesDTO>(queryResultFeaturesJson) :
                null;
        }
        catch
        {
            return null;
        }
    }

    private CoordinateFeaturesDTO TrySerializeCoordinateFeatures(string coordinateResultFeaturesJson)
    {
        try
        {
            return !String.IsNullOrWhiteSpace(coordinateResultFeaturesJson) ?
                JSerializer.Deserialize<CoordinateFeaturesDTO>(coordinateResultFeaturesJson) :
                null;
        }
        catch
        {
            return null;
        }
    }

    private ChainageFeaturesDTO TrySerializeChainageFeatures(string chainageResultFeaturesJson)
    {
        try
        {
            return !String.IsNullOrWhiteSpace(chainageResultFeaturesJson) ?
                JSerializer.Deserialize<ChainageFeaturesDTO>(chainageResultFeaturesJson) :
                null;
        }
        catch
        {
            return null;
        }
    }

    private System.Text.Encoding GetCsvEncoding()
    {
        return _config.DefaultTextDownloadEncoding();
    }

    async private Task<IActionResult> PrintResponse(ApiBaseController controller, string name, string filePath)
    {
        var httpRequest = controller.Request;
        var resultFormat = httpRequest.Form["result_format"];

        if ("base64".Equals(resultFormat, StringComparison.InvariantCultureIgnoreCase))
        {
            var ms = await filePath.BytesFromUri(_requestContext.Http);

            filePath.TryDelete();

            return await controller.JsonObject(new
            {
                name = name,
                base64 = Convert.ToBase64String(ms.ToArray())
            });
        }

        return await controller.JsonObject(new
        {
            name = name,
            downloadid = _crypto.EncryptTextDefault(new FileInfo(filePath).Name, CryptoResultStringType.Hex)
        });
    }

    #endregion

    #region Pdf Helper

    private simpleRect GetPageSize(PageSize ps, PageOrientation po)
    {
        return GetPageSize(ps, po, 0);
    }
    private simpleRect GetPageSize(PageSize ps, PageOrientation po, int dpi)
    {
        simpleRect rect;
        rect.width = 0;
        rect.height = 0;
        double dpmm = dpi / 25.4, rand = 0;
        if (dpi == 0)
        {
            dpmm = 1.0;
        }

        switch (ps)
        {
            case PageSize.A4:  // 210x297
                rect.width = (int)((210.0 - rand) * dpmm);
                rect.height = (int)((297.0 - rand) * dpmm);
                break;
            case PageSize.A3: // 297x420
                rect.width = (int)((297.0 - rand) * dpmm);
                rect.height = (int)((420.0 - rand) * dpmm);
                break;
            case PageSize.A2: // 420x594
                rect.width = (int)((420.0 - rand) * dpmm);
                rect.height = (int)((594.0 - rand) * dpmm);
                break;
            case PageSize.A1: // 594x841
                rect.width = (int)((594.0 - rand) * dpmm);
                rect.height = (int)((841.0 - rand) * dpmm);
                break;
            case PageSize.A0: // 841x1189
                rect.width = (int)((841.0 - rand) * dpmm);
                rect.height = (int)((1189.0 - rand) * dpmm);
                break;

            case PageSize.A4_A3:
                rect.width = (int)((420.0 - rand) * dpmm);
                rect.height = (int)((297.0 - rand) * dpmm);
                break;
            case PageSize.A4_A2:
                rect.width = (int)((594.0 - rand) * dpmm);
                rect.height = (int)((297.0 - rand) * dpmm);
                break;
            case PageSize.A4_A1:
                rect.width = (int)((841.0 - rand) * dpmm);
                rect.height = (int)((297.0 - rand) * dpmm);
                break;
            case PageSize.A4_A0:
                rect.width = (int)((1189.0 - rand) * dpmm);
                rect.height = (int)((297.0 - rand) * dpmm);
                break;

            case PageSize.A3_A2:
                rect.width = (int)((594.0 - rand) * dpmm);
                rect.height = (int)((420.0 - rand) * dpmm);
                break;
            case PageSize.A3_A1:
                rect.width = (int)((841.0 - rand) * dpmm);
                rect.height = (int)((420.0 - rand) * dpmm);
                break;

            case PageSize.A2_A1:
                rect.width = (int)((841.0 - rand) * dpmm);
                rect.height = (int)((594.0 - rand) * dpmm);
                break;
        }
        if (po == PageOrientation.Landscape)
        {
            int w = rect.width;
            rect.width = rect.height;
            rect.height = w;
        }

        return rect;
    }

    private string WorldFileExtension(string format)
    {
        return format == "png" ? "pgw" : "jgw";
    }

    #endregion
}
