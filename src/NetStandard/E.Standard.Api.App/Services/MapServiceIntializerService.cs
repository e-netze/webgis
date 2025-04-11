using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services.Cms;
using E.Standard.Caching.Abstraction;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Security;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Compare;
using E.Standard.OGC.Schema;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using E.Standard.WebMapping.GeoServices;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Services;

public class MapServiceInitializerService
{
    private readonly ConfigurationService _config;
    private readonly IUrlHelperService _urlHelper;
    private readonly ITempDataObjectCache _objectCache;
    private readonly ICryptoService _crypto;
    private readonly IRequestContext _requestContext;
    private readonly CmsDocumentsService _cmsDocuments;

    public MapServiceInitializerService(ConfigurationService config,
                                        IUrlHelperService urlHelper,
                                        ITempDataObjectCache objectCache,
                                        ICryptoService crypto,
                                        IRequestContext requestContext,
                                        CmsDocumentsService cmsDocuments)
    {
        _config = config;
        _urlHelper = urlHelper;
        _objectCache = objectCache;
        _crypto = crypto;
        _requestContext = requestContext;
        _cmsDocuments = cmsDocuments;
    }

    public IMap Map(IRequestContext requestContext, CmsDocument.UserIdentification ui, string name = "")
    {
        string appConfigPath = ApiGlobals.AppConfigPath;
        Map map = new Map(name);

        map.Environment.SetUserValue("username", ui?.Username ?? String.Empty /*"anonymous"*/);

        map.Environment.SetUserValue(webgisConst.OutputPath, _urlHelper.OutputPath());
        map.Environment.SetUserValue(webgisConst.OutputUrl, _urlHelper.OutputUrl());
        map.Environment.SetUserValue(webgisConst.EtcPath, ApiGlobals.AppEtcPath);

        map.Environment.SetUserValue(webgisConst.SessionId, ui?.PublicKey ?? String.Empty);
        map.Environment.SetUserValue(webgisConst.AppConfigPath, ApiGlobals.AppConfigPath);
        map.Environment.SetUserValue(webgisConst.UserName, ui?.Username ?? String.Empty);

        map.Environment.SetUserValue(webgisConst.UserIdentification, ui);
        map.Environment.SetUserValue(webgisConst.ShowWarningInPrintLayout, _config.ShowWarningsInPrintLayout());

        return map;
    }

    public IMapService ServiceInstance(CmsDocument cms, string cmsName, CmsLink serviceLink, string appConfigPath)
    {
        try
        {
            IMapService service = null;

            string serviceName = serviceLink.Target.LoadString("service");
            string authUser = String.Empty, authPwd = String.Empty, token = String.Empty;

            if (serviceLink.Target.ParentNode.Name == "ims")
            {
                var imsService = new WebMapping.GeoServices.AXL.AxlService();

                imsService.Encoding = System.Text.Encoding.UTF8;
                imsService.ExtraCheckUmlaute = false;
                imsService.Umlaute2Wildcard = false;
                //if (map.SpatialReference != null &&
                //    (ServiceProjection)serviceLink.Load("projmethode", (int)ServiceProjection.none) == ServiceProjection.Map)
                //{
                //    ((E.WebMapping.AXL.Service)service).FeatureCoordsys =
                //    ((E.WebMapping.AXL.Service)service).FilterCoordsys = "id='" + map.SpatialReference.Id + "'"; // "string='" + map.SpatialReference.WKT.Replace("\"", "&quot;") + "'";
                //}
                authUser = serviceLink.Target.LoadString("user");
                authPwd = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("pwd"), "imsservice");
                token = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("token"), "imsservice");
                imsService.SetImageFormat((ServiceImageFormat)serviceLink.Load("imageformat", (int)ServiceImageFormat.Default));
                imsService.UseFixRefScale = (bool)serviceLink.Load("usefixrefscale", false);
                imsService.FixRefScale = (double)serviceLink.Load("fixrefscale", 0.0);

                string overrideLocale = serviceLink.Target.LoadString("overridelocale");

                if (!String.IsNullOrEmpty(overrideLocale))
                {
                    imsService.OverrideLocale = overrideLocale;
                }

                imsService.CreatePresentationsDynamic = ((ServiceDynamicPresentations)serviceLink.Target.Load("dynamic_presentations", (int)ServiceDynamicPresentations.Manually));
                imsService.CreateQueriesDynamic = ((ServiceDynamicQueries)serviceLink.Target.Load("dynamic_queries", (int)ServiceDynamicQueries.Manually));
                imsService.ImageServiceType = ((ImageServiceType)serviceLink.Target.Load("service_type", (int)ImageServiceType.Normal));

                service = imsService;
            }
            else if (serviceLink.Target.ParentNode.Name == "mapserver" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "arcgisserver")
            {
                serviceName = serviceLink.Target.LoadString("serviceurl");
                authUser = serviceLink.Target.LoadString("user");
                authPwd = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("pwd"), "agsservice");
                token = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("token"), "agsservice");

                if (serviceName.ToLower().Contains("/rest/") || serviceName.ToLower().Contains("/krako/proxy/"))
                {
                    var mapService = new WebMapping.GeoServices.ArcServer.Rest.MapService();

                    mapService.SetImageFormat((ServiceImageFormat)serviceLink.Load("imageformat", (int)ServiceImageFormat.Default));
                    mapService.TokenExpiration = ((int)serviceLink.Target.Load("expiration", 3600));
                    mapService.SealedLayers =
                        (DynamicDehavior)serviceLink.Target.Load("dynamic_behavior", (int)DynamicDehavior.AutoAppendNewLayers) == DynamicDehavior.SealedLayers_UseServiceDefaults;

                    mapService.ExportMapFormat = ((AGSExportMapFormat)serviceLink.Target.Load("exportmapformat", (int)AGSExportMapFormat.Json));
                    mapService.CreatePresentationsDynamic = ((ServiceDynamicPresentations)serviceLink.Target.Load("dynamic_presentations", (int)ServiceDynamicPresentations.Manually));
                    mapService.CreateQueriesDynamic = ((ServiceDynamicQueries)serviceLink.Target.Load("dynamic_queries", (int)ServiceDynamicQueries.Manually));
                    mapService.ImageServiceType = ((ImageServiceType)serviceLink.Target.Load("service_type", (int)ImageServiceType.Normal));

                    service = mapService;
                }
                else
                {
                    throw new NotImplementedException("AGS Soap Services not implemented");
                    //service = new E.WebMapping.ArcServer.Service();
                    //((E.WebMapping.ArcServer.Service)service).SetImageFormat((ServiceImageFormat)serviceLink.Load("imageformat", (int)ServiceImageFormat.Default));
                    //((E.WebMapping.ArcServer.Service)service).GetSelectionMethod = (AGSGetSelectionMothod)serviceLink.Load("getselectionmethod", (int)AGSGetSelectionMothod.Modern);
                }
            }
            else if (serviceLink.Target.ParentNode.Name == "tileservice" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "arcgisserver")
            {
                service = new WebMapping.GeoServices.ArcServer.Rest.TileGridService(
                    serviceLink.Target.LoadString("serviceconfigurl"),
                    serviceLink.Target.LoadString("tileurl"),
                    cms.SelectSingleNode(null, serviceLink.Target.NodeXPath + "/themes/*", "id", "0"),
                    (bool)serviceLink.Target.Load("hide_beyond_maxlevel", false));

                authUser = serviceLink.Target.LoadString("user");
                authPwd = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("pwd"), "agsservice");
                token = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("token"), "agsservice");
            }
            else if (serviceLink.Target.ParentNode.Name == "wmtsservice" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "arcgisserver")
            {
                var wmtsService = new WebMapping.GeoServices.ArcServer.Rest.WmtsService(
                    serviceLink.Target.LoadString("server"),
                    serviceLink.Target.LoadString("layer"),
                    serviceLink.Target.LoadString("tilematrixset"),
                    serviceLink.Target.LoadString("style"),
                    serviceLink.Target.LoadString("imageformat"),
                    serviceLink.Target.LoadString("resourceurls").Split(';'),
                    (int)serviceLink.Target.Load("maxlevel", -1),
                    (bool)serviceLink.Target.Load("hide_beyond_maxlevel", false));

                wmtsService.TokenExpiration = ((int)serviceLink.Target.Load("token_expiration", 60));

                authUser = serviceLink.Target.LoadString("user");
                authPwd = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("pwd"), "agsservice");
                token = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("token"), "agsservice");

                service = wmtsService;
            }
            else if (serviceLink.Target.ParentNode.Name == "imageserverservice" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "arcgisserver")
            {
                var imageServerService = new WebMapping.GeoServices.ArcServer.Rest.ImageServerService();

                imageServerService.ServiceUrl = serviceLink.Target.LoadString("serviceurl");
                imageServerService.ImageFormat = (ArcIS_ImageFormat)serviceLink.Target.Load("imageformat", (int)ArcIS_ImageFormat.jpgpng);
                imageServerService.PixelType = (ArcIS_PixelType)serviceLink.Target.Load("pixeltype", (int)ArcIS_PixelType.UNKNOWN);
                imageServerService.NoData = serviceLink.Target.LoadString("nodata");
                imageServerService.NoDataInterpretation = (ArcIS_NoDataInterpretation)serviceLink.Target.Load("nodatainterpretation", ArcIS_NoDataInterpretation.esriNoDataMatchAny);
                imageServerService.Interpolation = (ArcIS_Interpolation)serviceLink.Target.Load("interpretation", ArcIS_Interpolation.RSP_BilinearInterpolation);
                imageServerService.CompressionQuality = serviceLink.Target.LoadString("compressqualitity");
                imageServerService.BandIDs = serviceLink.Target.LoadString("bandids");
                imageServerService.MosaicRule = serviceLink.Target.LoadString("mosaicrule");
                imageServerService.RenderingRule = serviceLink.Target.LoadString("renderingrule");
                imageServerService.RenderingRuleIdentify = serviceLink.Target.LoadString("renderingrule_identify");
                imageServerService.ImageServiceType = ((ImageServiceType)serviceLink.Target.Load("service_type", (int)ImageServiceType.Normal));
                imageServerService.CreatePresentationsDynamic = ((ServiceDynamicPresentations)serviceLink.Target.Load("dynamic_presentations", (int)ServiceDynamicPresentations.Manually));
                imageServerService.CreateQueriesDynamic = ((ServiceDynamicQueries)serviceLink.Target.Load("dynamic_queries", (int)ServiceDynamicQueries.Manually));
                imageServerService.PixelAliasname = serviceLink.Target.LoadString("pixel_aliasname");

                service = imageServerService;

                serviceName = serviceLink.Target.LoadString("service");
                authUser = serviceLink.Target.LoadString("user");
                authPwd = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("pwd"), "agsservice");
                token = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("token"), "agsservice");
            }
            else if (serviceLink.Target.ParentNode.Name == "wms" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "ogc")
            {
                WMS_Version wms_version = (WMS_Version)serviceLink.Target.Load("version", (int)WMS_Version.version_1_1_1);

                var wmsService = new WebMapping.GeoServices.OGC.WMS.WmsService(wms_version,
                    (string)serviceLink.Target.Load("getmapformat", "image/png"),
                    (string)serviceLink.Target.Load("getfeatureinfoformat", "text/html"),
                    (WMS_LayerOrder)serviceLink.Target.Load("layerorder", (int)WMS_LayerOrder.Up),
                    (WMS_Vendor)serviceLink.Target.Load("vendor", (int)WMS_Vendor.Unknown));

                authUser = serviceLink.Target.LoadString("user");
                authPwd = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("pwd"), "wmsservice");
                token = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("token"), "wmsservice");

                if (!String.IsNullOrEmpty(serviceLink.Target.LoadString("cert")))
                {
                    /*
                    ((E.WebMapping.OGC.WMS.Service)service).X509Certificate =
                        Certification.X509Certificate(map.MapSession,
                        serviceLink.Target.LoadString("cert"),
                        Crypto.Decrypt(serviceLink.Target.LoadString("certpwd"), "WmsServiceCertificatePassword"));
                     * */
                }
                if (!String.IsNullOrEmpty(serviceLink.Target.LoadString("TicketServer")))
                {
                    wmsService.TicketServer = serviceLink.Target.LoadString("TicketServer");
                }
                wmsService.GetFeatureInfoFeatureCount = (int)serviceLink.Target.Load("featurecount", 30);

                wmsService.CreatePresentationsDynamic = ((ServiceDynamicPresentations)serviceLink.Target.Load("dynamic_presentations", (int)ServiceDynamicPresentations.Manually));
                wmsService.CreateQueriesDynamic = ((ServiceDynamicQueries)serviceLink.Target.Load("dynamic_queries", (int)ServiceDynamicQueries.Manually));
                wmsService.ImageServiceType = ((ImageServiceType)serviceLink.Target.Load("service_type", (int)ImageServiceType.Normal));
                wmsService.SealedLayers =
                       (DynamicDehavior)serviceLink.Target.Load("dynamic_behavior", (int)DynamicDehavior.AutoAppendNewLayers) == DynamicDehavior.SealedLayers_UseServiceDefaults;

                service = wmsService;
            }
            else if (serviceLink.Target.ParentNode.Name == "wfs" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "ogc")
            {
                WFS_Version wms_version = (WFS_Version)serviceLink.Target.Load("version", (int)WFS_Version.version_1_0_0);
                service = new WebMapping.GeoServices.OGC.WFS.WfsService(wms_version);
                ((WebMapping.GeoServices.OGC.WFS.WfsService)service).InterpretSrsAxix = (bool)serviceLink.Target.Load("interpretsrsaxis", true);
                authUser = serviceLink.Target.LoadString("user");
                authPwd = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("pwd"), "wmsservice");
                token = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("token"), "wmsservice");
                if (!String.IsNullOrEmpty(serviceLink.Target.LoadString("cert")))
                {
                    /*
                    ((E.WebMapping.OGC.WFS.Service)service).X509Certificate =
                        Certification.X509Certificate(map.MapSession,
                        serviceLink.Target.LoadString("cert"),
                        Crypto.Decrypt(serviceLink.Target.LoadString("certpwd"), "WmsServiceCertificatePassword"));
                     * */
                }
                ((WebMapping.GeoServices.OGC.WFS.WfsService)service).GetFeatureInfoFeatureCount = (int)serviceLink.Target.Load("featurecount", 30);
            }
            else if (serviceLink.Target.ParentNode.Name == "wmts" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "ogc")
            {
                service = new WebMapping.GeoServices.OGC.WMTS.WmtsService(
                    serviceLink.Target.LoadString("server"),
                    serviceLink.Target.LoadString("layer"),
                    serviceLink.Target.LoadString("tilematrixset"),
                    serviceLink.Target.LoadString("style"),
                    serviceLink.Target.LoadString("imageformat"),
                    serviceLink.Target.LoadString("resourceurls").Split(';'),
                    (int)serviceLink.Target.Load("maxlevel", -1),
                    (bool)serviceLink.Target.Load("hide_beyond_maxlevel", false));

                authUser = serviceLink.Target.LoadString("user");
                authPwd = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("pwd"), "wmtsservice");
                token = CmsCryptoHelper.Decrypt(serviceLink.Target.LoadString("token"), "wmtsservice");
                if (!String.IsNullOrEmpty(serviceLink.Target.LoadString("TicketServer")))
                {
                    ((WebMapping.GeoServices.OGC.WMTS.WmtsService)service).TicketServer = serviceLink.Target.LoadString("TicketServer");
                }
            }
            else if (serviceLink.Target.ParentNode.Name == "wmsc" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "ogc")
            {
                service = new WebMapping.GeoServices.OGC.WMSC.WmscService(
                    serviceLink.Target.LoadString("server"),
                    serviceLink.Target.LoadString("tiledlayer"),
                    serviceLink.Target.LoadString("tiledcrs"),
                    serviceLink.Target.LoadString("tileurl"));
            }
            else if (serviceLink.Target.ParentNode.Name == "generaltilecache" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "miscellaneous")
            {
                service = new WebMapping.GeoServices.Tiling.GeneralTileService(
                    cms.SelectSingleNode(null, serviceLink.Target.NodeXPath + "/properties"),
                    cms.SelectSingleNode(null, serviceLink.Target.NodeXPath + "/themes/*", "id", "0"));
            }
            else if (serviceLink.Target.ParentNode.Name == "generalvectortilecache" &&
                    serviceLink.Target.ParentNode.ParentNode.Name == "miscellaneous")
            {
                string fallbackService = (cms.SelectSingleNode(null, serviceLink.Target.NodeXPath + "/fallback/*") as CmsLink)?.Target?.Url ?? "";
                if (!String.IsNullOrEmpty(fallbackService) && !string.IsNullOrEmpty(cmsName) && "cms".Equals(cmsName, StringComparison.OrdinalIgnoreCase) == false)
                {
                    fallbackService += $"@{cmsName}";
                }
                service = new WebMapping.GeoServices.Tiling.GeneralVectorTileService(
                       cms.SelectSingleNode(null, serviceLink.Target.NodeXPath + "/properties"),
                       cms.SelectSingleNode(null, serviceLink.Target.NodeXPath + "/themes/*", "id", "0"),
                       fallbackService
                   );
            }
            else if (serviceLink.Target.ParentNode.Name == "mapservicecollections")
            {
                service = new MapServiceCollectionService();

                var cmsHlp = new CmsHlp(cms, null);
                var collectionItems = new List<MapServiceCollectionService.Item>();

                foreach (var serviceNode in cmsHlp.MapServiceCollectionServiceNodes(serviceLink.Target))
                {
                    if (serviceNode is CmsLink mapServiceLink)
                    {
                        if (String.IsNullOrEmpty(serviceLink.Target?.Url))
                        {
                            continue;
                        }

                        collectionItems.Add(new MapServiceCollectionService.Item(
                            url: String.IsNullOrWhiteSpace(cmsName) ? mapServiceLink.Target.Url : $"{mapServiceLink.Target.Url}@{cmsName}",
                            layerVisibility: (MapServiceLayerVisibility)mapServiceLink.Load("layer_visibility", (int)MapServiceLayerVisibility.ServiceDefaults)
                            )
                        );
                    }
                }

                ((MapServiceCollectionService)service).Items = collectionItems.ToArray();
            }
            else if (serviceLink.Target.ParentNode.Name == "servicecollection" &&
                     serviceLink.Target.ParentNode.ParentNode.Name == "miscellaneous")
            {
                #region obsolete

                throw new NotImplementedException();

                //service = new ApiCollectionService(_cmsDocuments, cmsName, serviceLink.Target.NodeXPath);
                //CmsNodeCollection childServiceLinks = cms.SelectNodes(null, serviceLink.Target.NodeXPath + "/services/*");
                //childServiceLinks.Reverse();

                //foreach (CmsLink childServiceLink in childServiceLinks)
                //{
                //    if (childServiceLink.Target.ParentNode.Name == "servicecollection" &&
                //        childServiceLink.Target.ParentNode.ParentNode.Name == "miscellaneous")
                //    {
                //        continue;
                //    }

                //    IMapService childService = ServiceInstance(cms, cmsName, childServiceLink, appConfigPath);
                //    if (childService == null)
                //    {
                //        continue;
                //    }

                //    string serviceUrlKey = String.IsNullOrWhiteSpace(cmsName) ? childService.Url : childService.Url + "@" + cmsName;
                //    childService.Url = serviceUrlKey;

                //    ((ApiCollectionService)service).AddService(childService);

                //    //childService.CollectionId = serviceLink.Target.LoadString("guid").ToLower();
                //    //map.Services.Add(childService);
                //    //((CollectionService)service).AddService(childService);  die Zuweisung der Dienste auf das CollectionService passiert im Map.Init()!!!
                //}

                #endregion
            }
            if (service == null)
            {
                return null;
            }

            double opacity = Math.Clamp((double)serviceLink.Load("opacity", 100.0), 0D, 100D);
            double opacityFactor = Math.Clamp((double)serviceLink.Load("opacity_factor", 1.0), 0D, 1D);
            service.InitialOpacity = (float)(opacity / 100.0);
            service.OpacityFactor = (float)opacityFactor;

            service.DiagnosticsWaringLevel = (ServiceDiagnosticsWarningLevel)serviceLink.Load("warninglevel", (int)ServiceDiagnosticsWarningLevel.Never);

            if (service is IServiceLegend serviceLegend)
            {
                serviceLegend.FixLegendUrl = (string)serviceLink.Load("legendurl", String.Empty);
                serviceLegend.LegendOptMethod = (LegendOptimization)serviceLink.Load("legendopt", LegendOptimization.None);
                serviceLegend.LegendOptSymbolScale = (double)serviceLink.Load("legendoptsymbolscale", 1000.0);
                serviceLegend.LegendVisible = false;
                serviceLegend.ShowServiceLegendInMap = (bool)serviceLink.Load("showinlegend", true);
            }
            if (service is IExportableOgcService exportableOgcService)
            {
                exportableOgcService.ExportWms = (bool)serviceLink.Load("exportwms", false);
            }
            service.Url = serviceLink.Target.Url;
            service.Timeout = (int)serviceLink.Load("timeout", 20);
            service.Name = String.IsNullOrEmpty(serviceLink.LoadString("tocdisplayname")) ? serviceLink.Target.AliasName : serviceLink.LoadString("tocdisplayname");
            service.MinScale = (double)serviceLink.Load("minscale", 0.0);
            service.MaxScale = (double)serviceLink.Load("maxscale", 0.0);
            service.CheckSpatialConstraints = (bool)serviceLink.Load("usewithspatialconstraintservice", false);
            service.ShowInToc = (bool)serviceLink.Load("showintoc", true);

            if (service is IServiceSupportedCrs serviceSupportedCrs)
            {
                string supportedCrs = serviceLink.LoadString("supportedcrs");
                if (!String.IsNullOrWhiteSpace(supportedCrs))
                {
                    string[] crs = supportedCrs.Split(',');
                    int[] srsids = new int[crs.Length];

                    for (int i = 0; i < crs.Length; i++)
                    {
                        srsids[i] = int.Parse(crs[i]);
                    }

                    serviceSupportedCrs.SupportedCrs = srsids;
                }
            }

            service.IsBaseMap = (bool)serviceLink.Load("isbasemap", false);
            service.BasemapType = (BasemapType)serviceLink.Load("basemaptype", (int)BasemapType.Normal);
            service.BasemapPreviewImage = serviceLink.LoadString("basemap_previewimageurl");

            if (service is IServiceProjection serviceProjection)
            {
                serviceProjection.ProjectionId = (int)serviceLink.Load("projid", -1);
                serviceProjection.ProjectionMethode = (ServiceProjectionMethode)serviceLink.Load("projmethode", (int)ServiceProjectionMethode.none);
            }
            if (service is IServiceDatumTransformations serviceDatumTransformations)
            {
                string datums = serviceLink.LoadString("datums");

                if (!String.IsNullOrWhiteSpace(datums))
                {
                    serviceDatumTransformations.DatumTransformations = datums.Split(',').Select(d => int.Parse(d)).ToArray();
                }
            }

            if (service is IServiceCopyrightInfo serviceCopyrightInfo)
            {
                serviceCopyrightInfo.CopyrightInfoId = serviceLink.LoadString("copyright");
                if (!String.IsNullOrWhiteSpace(((IServiceCopyrightInfo)service).CopyrightInfoId) && !String.IsNullOrWhiteSpace(cmsName) && cmsName != "cms")
                {
                    serviceCopyrightInfo.CopyrightInfoId += "@" + cmsName;
                }
            }

            #region Überprüfen, ob Server lizenziert

            string serverName = serviceLink.Target.LoadString("server");
            if (String.IsNullOrEmpty(serverName) && service != null)
            {
                serverName = service.Server;
            }

            if (serverName.Contains("/"))
            {
                if (serverName.StartsWith("#") ||
                    serverName.StartsWith("$") ||
                    serverName.StartsWith("+") ||
                    serverName.StartsWith("-"))
                {
                    serverName = serverName.Substring(1, serverName.Length - 1);
                }

                try
                {
                    Uri uri = new Uri(serverName);
                    serverName = uri.Host;
                }
                catch { }
            }

            #endregion

            if (/*!serverIsLicensed*/ false)
            {
                //service = new WebMapping.Services.ExceptionHandling.Service(
                //    new Exception("Dienste vom Server " + serverName + " sind nicht lizenziert!"));
            }
            else
            {
                serverName = serviceLink.Target.LoadString("server");
            }

            service.PreInit(serviceLink.Target.LoadString("guid").ToLower(),
                           serverName,
                           serviceName,
                           authUser, authPwd, token, appConfigPath,
                           CmsHlp.ServiceThemes(cms, serviceLink.Target));

            if (service is WebMapping.GeoServices.Tiling.TileService)
            {
                ((WebMapping.GeoServices.Tiling.TileService)service).GridRendering = (TileGridRendering)serviceLink.Target.Load("rendering", (int)TileGridRendering.Quality);
            }

            return service;
        }
        catch (Exception /*ex*/)
        {
            /*
            if (map != null && map.ExceptionLogger is IExceptionLogger)
                ((IExceptionLogger)map.ExceptionLogger).LogException(ex);
             * */
            return null;
        }
    }

    #region CustomServices

    const string CustomServicePrefix = "_custom_";

    async public Task<IMapService> CustomServiceInstanceAsync(string serviceId, string url, string displayName, string user, string password, CmsDocument.UserIdentification ui)
    {
        string serviceName = url, serverName = url;

        IMapService service = _objectCache.Get(serviceId) as IMapService;
        if (service != null)
        {
            return service;
        }

        url = url.Trim();

        if (url.ToLower().EndsWith("/mapserver"))  // AGS MapServer Service
        {
            var mapService = new WebMapping.GeoServices.ArcServer.Rest.MapService();

            mapService.SetImageFormat(ServiceImageFormat.Default);
            mapService.TokenExpiration = 60;

            mapService.ExportMapFormat = AGSExportMapFormat.Json;
            mapService.ImageServiceType = ImageServiceType.Normal;

            service = mapService;

            serverName = $"{new Uri(url).Scheme}://{new Uri(url).Host}";
        }
        else if (url.ToLower().EndsWith("/imageserver"))  // AGS ImageServer Service
        {
            var imageServerService = new WebMapping.GeoServices.ArcServer.Rest.ImageServerService();

            imageServerService.ServiceUrl = url;
            imageServerService.ImageFormat = ArcIS_ImageFormat.jpgpng;
            imageServerService.PixelType = ArcIS_PixelType.UNKNOWN;
            imageServerService.NoData = String.Empty;
            imageServerService.NoDataInterpretation = ArcIS_NoDataInterpretation.esriNoDataMatchAny;
            imageServerService.Interpolation = ArcIS_Interpolation.RSP_BilinearInterpolation;
            imageServerService.CompressionQuality = String.Empty;
            imageServerService.BandIDs = String.Empty;
            imageServerService.MosaicRule = String.Empty;
            imageServerService.RenderingRule = String.Empty;
            imageServerService.ImageServiceType = ImageServiceType.Normal;
            imageServerService.CreatePresentationsDynamic = ServiceDynamicPresentations.Manually;
            imageServerService.CreateQueriesDynamic = ServiceDynamicQueries.Manually;
            imageServerService.PixelAliasname = String.Empty;

            service = imageServerService;

            serviceName = imageServerService.ImageServiceName(url);
            serverName = $"{new Uri(url).Scheme}://{new Uri(url).Host}";
        }
        else // WMS
        {
            WMS_Version wms_version = WMS_Version.version_1_1_1;

            if (url.Contains("?"))
            {
                var wmsQueryString = System.Web.HttpUtility.ParseQueryString(string.Empty);

                var queryString = System.Web.HttpUtility.ParseQueryString(url.Substring(url.IndexOf("?") + 1));
                foreach (var urlParameter in queryString.AllKeys)
                {
                    switch (urlParameter.ToLower())
                    {
                        case "service":
                            if (queryString[urlParameter]?.ToLower() != "wms")
                            {
                                throw new Exception($"Unknown service: {queryString[urlParameter]}. Must bei WMS!");
                            }

                            break;
                        case "request":
                            break;
                        case "version":
                            if (queryString[urlParameter] == "1.1.1")
                            {
                                wms_version = WMS_Version.version_1_1_1;
                            }
                            else if (queryString[urlParameter] == "1.3.0")
                            {
                                wms_version = WMS_Version.version_1_3_0;
                            }
                            else
                            {
                                throw new Exception($"Unsupported wms version {queryString[urlParameter]}. Use 1.1.1 or 1.3.0!");
                            }
                            break;
                        default:
                            wmsQueryString.Add(urlParameter, queryString[urlParameter]);
                            break;
                    }
                }

                serverName = serviceName = $"{url.Split('?')[0]}?{wmsQueryString}";
            }

            var wmsService = new WebMapping.GeoServices.OGC.WMS.WmsService(
                         wms_version,
                         "image/png",
                         "text/html",
                         WMS_LayerOrder.Up,
                         WMS_Vendor.Unknown);

            wmsService.GetFeatureInfoFeatureCount = 30;
            wmsService.ImageServiceType = ImageServiceType.Normal;

            service = wmsService;
        }

        if (service != null)
        {
            if (service is IDynamicService dynamicService)
            {
                dynamicService.CreatePresentationsDynamic = ServiceDynamicPresentations.Auto;
                dynamicService.CreateQueriesDynamic = ServiceDynamicQueries.Auto;
            }

            service.PreInit(Guid.NewGuid().ToString("N").ToLower(),
                            serverName,
                            serviceName,
                            user, password,
                            String.Empty, ApiGlobals.AppConfigPath,
                            null);

            if (service is WebMapping.GeoServices.Tiling.TileService tileService)
            {
                tileService.GridRendering = TileGridRendering.Quality;
            }

            if (!await service.InitAsync(this.Map(_requestContext, ui), _requestContext) || service.HasInitialzationErrors())
            {
                throw new Exception(service.ErrorAndDiagnosticMessage());
            }

            service.Name = displayName.OrTake(service.Name).OrTake(service.ServiceShortname);
            service.Url = serviceId;

            _objectCache.Set(serviceId, service);
        }

        return service;
    }

    async public Task<IMapService> GetCustomServiceByUrlAsync(string serviceId, IMap parent, CmsDocument.UserIdentification ui, NameValueCollection requestFormParameters)
    {
        if (!IsCustomService(serviceId))
        {
            return null;
        }

        var service = _objectCache.Get(serviceId) as IMapService;
        if (service == null && requestFormParameters != null && !String.IsNullOrEmpty(requestFormParameters[$"custom.{serviceId}.connection"]))
        {
            var connectionParameter = requestFormParameters[$"custom.{serviceId}.connection"];

            string connectionString = String.Empty;
            try
            {
                connectionString = _crypto.StaticDefaultDecrypt(connectionParameter);
            }
            catch { }

            if (!String.IsNullOrEmpty(connectionString))
            {
                service = await CustomServiceInstanceAsync(
                    serviceId,
                    connectionString.ExtractConnectionStringValue("url"),
                    connectionString.ExtractConnectionStringValue("name"),
                    connectionString.ExtractConnectionStringValue("usr"),
                    connectionString.ExtractConnectionStringValue("pwd"),
                    ui);
            }
        }

        if (service != null)
        {
            service = service.Clone(parent);
        }

        return service;
    }

    public string GenerateNewCustomServiceId() => $"{CustomServicePrefix}{Guid.NewGuid():N}";

    public string EncodeCustomServiceConnectionString(string url, string displayName, string user, string password, CmsDocument.UserIdentification ui)
    {
        return $"{_crypto.StaticDefaultEncrypt($"url={url};name={displayName};usr={user};pwd={password}", CryptoResultStringType.Hex)}";
    }

    public bool IsCustomService(string serviceId) => serviceId != null && serviceId.StartsWith(CustomServicePrefix) && !serviceId.Contains("@");

    #endregion

    public void LogException(IRequestContext requestContext, Exception ex, string command, IMap map = null, string server = "", string service = "")
    {
        try
        {
            if (map == null)
            {
                map = Map(requestContext, null, String.Empty);
            }

            requestContext.GetRequiredService<IExceptionLogger>()
                .LogException(map, server, service, command, ex);
        }
        catch { }
    }
}