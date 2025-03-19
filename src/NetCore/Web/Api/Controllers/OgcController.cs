using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Reflection;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Ogc;
using Api.Core.AppCode.Services.Rest;
using Api.Core.Models.Ogc;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Exceptions.Ogc;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Reflection;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Api.Core.Controllers;

[ApiAuthentication(ApiAuthenticationTypes.CustomOgcTicket)]
public class OgcController : ApiBaseController
{
    private readonly ILogger<OgcController> _logger;
    private readonly EtagService _etag;
    private readonly OgcRequestService _ogcRequest;
    private readonly RestHelperService _restHelper;
    private readonly HttpRequestContextService _httpRequestContext;
    private readonly CacheService _cache;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly ConfigurationService _config;
    private readonly IRequestContext _requestContext;
    private readonly ICryptoService _crypto;
    private readonly UrlHelperService _urlHelper;
    private readonly IOgcPerformanceLogger _ogcPerformanceLogger;
    private readonly IEnumerable<ICustomTokenService> _tokenServices;

    public OgcController(ILogger<OgcController> logger,
                         EtagService etag,
                         OgcRequestService ogcRequest,
                         RestHelperService restHelper,
                         HttpRequestContextService httpRequestContext,
                         CacheService cache,
                         MapServiceInitializerService mapServiceInitializer,
                         UrlHelperService urlHelper,
                         ConfigurationService config,
                         IRequestContext requestContext,
                         ICryptoService crypto,
                         IOgcPerformanceLogger ogcPerformanceLogger,
                         IEnumerable<ICustomApiService> customServices = null,
                         IEnumerable<ICustomTokenService> tokenServices = null)
        : base(logger, urlHelper, requestContext.Http, customServices)
    {
        _logger = logger;
        _etag = etag;
        _ogcRequest = ogcRequest;
        _restHelper = restHelper;
        _httpRequestContext = httpRequestContext;
        _cache = cache;
        _mapServiceInitializer = mapServiceInitializer;
        _config = config;
        _requestContext = requestContext;
        _crypto = crypto;
        _urlHelper = urlHelper;
        _tokenServices = tokenServices;
        _ogcPerformanceLogger = ogcPerformanceLogger;
    }

    [Etag(appendResponseHeaders: false)]
    async public Task<IActionResult> Index(string id)
    {
        CmsDocument.UserIdentification ui = null;
        var arguments = new NameValueCollection();

        try
        {
            #region OGC Arguments (lowerCase)

            foreach (var key in this.Request.Query.Keys)
            {
                if (key == null || String.IsNullOrEmpty(this.Request.Query[key]))
                {
                    continue;
                }

                switch (key.ToLower())
                {
                    case "service":
                    case "request":
                    case "version":
                        string[] wmsArg = this.Request.Query[key].ToString().ToLower().Split(',');
                        arguments[key.ToLower()] = wmsArg[wmsArg.Length - 1];
                        break;
                    default:
                        arguments[key.ToLower()] = this.Request.Query[key];
                        break;
                }
            }

            #endregion

            string appendToServiceAbstract = String.Empty;

            try
            {
                ui = User.ToUserIdentification(acceptedAuthenticationTypes: ApiAuthenticationTypes.CustomOgcTicket,
                                               throwExceptions: true);
            }
            catch (OgcNotAuthorizedException ogcNotAuthEx)
            {
                if (arguments["request"] != "getcapabilities")
                {
                    throw;
                }

                appendToServiceAbstract += _ogcRequest.OgcExceptionMessage(ogcNotAuthEx, id, arguments, ui);
            }

            if (String.IsNullOrWhiteSpace(id))
            {
                List<IMapService> wmsServices = new List<IMapService>();
                foreach (IMapService service in _cache.GetServices(ui))
                {
                    if (_cache.IsWmsExportable(service.Url, ui))
                    //if (service is IExportableOgcService && ((IExportableOgcService)service).ExportWms == true)
                    {
                        wmsServices.Add(service);
                    }
                }

                var token = _tokenServices.GetCustomToken(this.Request);

                return ViewResult(new OgcIndexModel(_ogcRequest.OgcOnlineResouce(Request))
                {
                    WmsServices = wmsServices.ToArray(),
                    LoginService = _ogcRequest.OgcLoginService(this.Request),
                    LogoutService = this.ActionUrl("logout", new { ogc_ticket_logout = _crypto.EncryptTextDefault(Request.Query["ogc_ticket"], E.Standard.Security.Cryptography.CryptoResultStringType.Hex) }),
                    Username = ui.Username,
                    //ServiceParameters = !String.IsNullOrWhiteSpace(Request.QueryString["ogc_ticket"]) ? "&ogc_ticket=" + Request.QueryString["ogc_ticket"] : String.Empty
                    CustomToken = token
                });
            }

            MapRestrictions mapRestrictions = null;

            #region Service

            List<IMapService> services = new List<IMapService>();
            List<ServiceInfoDTO> serviceInfos = new List<ServiceInfoDTO>();
            Dictionary<string, string> transformations = new Dictionary<string, string>();
            string srs = String.IsNullOrWhiteSpace(arguments["srs"]) ? arguments["crs"] : arguments["srs"];
            if (!String.IsNullOrWhiteSpace(srs) && srs.Contains(":"))
            {
                srs = srs.Split(':')[1];
            }

            var map = _mapServiceInitializer.Map(_requestContext, ui);
            foreach (string serviceId in id.Split(','))
            {
                IMapService service = await _cache.GetService(serviceId, map, null, _urlHelper);  // hier immer "null" übergeben, weil beim GetCapabilities keine Anmeldung zwingend notwendig sein sollte (für Inspire). Der Fehler für die fehlende Anmeldung kommt dann erst im Kartenbild...
                if (service == null)
                {
                    throw new OgcArgumentException("Unknown service");
                }

                ServiceInfoDTO serviceInfo = _restHelper.CreateServiceInfo(this, service, arguments["request"] == "getcapabilities" ? null : ui);  // Hier mit UI abfragen, weil dann auch nur die erlauben Queries im Dienst sind. Bei GetCapabilities wieder null...

                if (serviceInfo == null)
                {
                    throw new OgcArgumentException("Unknown service info. Service '" + serviceId + "' is not authorized/available");
                }

                if (arguments["request"] == "getcapabilities")
                {
                    if (!_cache.IsWmsExportable(serviceInfo, null))  // Hier check, ob erlaubt. GetCapabilites immer erlaubt -> siehe oben
                    {
                        throw new OgcException("Service '" + serviceId + "' is not available for ogc wms export");
                    }

                    if (!_cache.IsWmsExportable(serviceInfo, ui))
                    {
                        appendToServiceAbstract += "Dieser Dienst ist geschützt. Für eine vollständige Nutzung ist eine Authentifizierung notwendig.";
                    }
                }
                else
                {
                    if (!_cache.IsWmsExportable(serviceInfo, ui))
                    {
                        throw new OgcNotAuthorizedException("Service '" + serviceId + "' is not authorized/available");
                    }
                }

                services.Add(service);
                serviceInfos.Add(serviceInfo);

                var polygonRestiction = _ogcRequest.GeometricRestrictions(HttpContext, serviceInfo, ui);
                if (polygonRestiction != null)
                {
                    if (mapRestrictions == null)
                    {
                        mapRestrictions = new MapRestrictions();
                    }

                    mapRestrictions.Add(service.ID, new ServiceRestirctions()
                    {
                        Bounds = polygonRestiction
                    });
                }
                if (!String.IsNullOrWhiteSpace(srs))
                {
                    string transformation = _ogcRequest.TransformationResrictions(serviceInfo, srs);
                    if (!String.IsNullOrWhiteSpace(transformation))
                    {
                        transformations.Add(E.Standard.WebGIS.CMS.webgisConst.Transformation + "-" + serviceInfo.id + "-" + srs, transformation);
                    }
                }
            }

            #endregion

            if (arguments["service"] == null)
            {
                throw new OgcArgumentException("SERVICE parameter missing");
            }

            if (arguments["version"] == null)
            {
                throw new OgcArgumentException("VERSION parameter missing");
            }

            if (arguments["request"] == null)
            {
                throw new OgcArgumentException("REQUEST parameter missing");
            }

            using (var logger = _ogcPerformanceLogger.Start(map, string.Empty, id, $"{arguments["service"]}/{arguments["request"]}", String.Empty))
            {
                try
                {
                    if (arguments["service"] == "wms" || arguments["service"].StartsWith("wms,"))
                    {
                        var wmsHelper = new E.Standard.Api.App.Ogc.WmsHelper(_ogcRequest.OgcOnlineResouce(this.Request),
                                                                             _config.OgcDefaultSupportedCrs());

                        switch (arguments["request"])
                        {
                            case "getcapabilities":
                                arguments.Add("append_to_service_abstract", appendToServiceAbstract);
                                return PlainView(wmsHelper.WMSGetCapabilities(serviceInfos.ToArray(), arguments), "text/xml");
                            case "getmap":
                                //logger.LoggingEntity.Level = 4;
                                return RawResponse(await wmsHelper.GetMapAsync(_requestContext,
                                                                               _httpRequestContext,
                                                                               _urlHelper,
                                                                               _cache,
                                                                               services.ToArray(),
                                                                               serviceInfos.ToArray(),
                                                                               arguments,
                                                                               ui,
                                                                               mapRestrictions,
                                                                               transformations), arguments["format"], null);
                            case "getfeatureinfo":
                                //logger.LoggingEntity.Level = 4;
                                return PlainView(await wmsHelper.GetFeatureInfo(_requestContext, _urlHelper, services.ToArray(), serviceInfos.ToArray(), arguments, ui, this.HttpContext), "text/html");
                            case "getlegendgraphic":
                                //logger.LoggingEntity.Level = 4;
                                return RawResponse(await wmsHelper.GetLegendGraphicAsync(_requestContext, _urlHelper, services.ToArray(), serviceInfos.ToArray(), arguments, ui), arguments["format"], null);
                            default:
                                throw new OgcArgumentException("not supported request: " + arguments["request"]);
                        }
                    }
                    else if (arguments["service"] == "wmts" || arguments["service"].StartsWith("wmts,"))
                    {
                        var wmtsHelper = new E.Standard.Api.App.Ogc.WmtsHelper(_ogcRequest.OgcOnlineResouce(this.Request));

                        switch (arguments["request"].Split('/')[0])   // ArcMap schickt:  "getcapabilities/1.0.0/wmtscapabilities.xml"
                        {
                            case "getcapabilities":
                                arguments.Add("append_to_service_abstract", appendToServiceAbstract);
                                return PlainView(wmtsHelper.WMTSGetCapabilities(serviceInfos.ToArray(), arguments), "text/xml");
                            case "gettile":
                                //logger.LoggingEntity.Level = 4;
                                byte[] data = await wmtsHelper.GetTile(_requestContext, services.ToArray(), serviceInfos.ToArray(), arguments, ui, mapRestrictions);
                                _etag.AppendEtag(HttpContext, DateTime.UtcNow.AddDays(1));

                                return RawResponse(data, arguments["format"], null);
                            default:
                                throw new OgcArgumentException("not supported request: " + arguments["request"]);
                        }
                    }

                    throw new Exception("not supported service: " + arguments["service"]);
                }
                catch (Exception ex)
                {
                    _requestContext.GetRequiredService<IExceptionLogger>()
                        .LogException(map, String.Empty, id, arguments["service"] + "/" + arguments["request"], ex);

                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            return _ogcRequest.HandleOgcException(this, ex, id, arguments, ui);
        }
    }

    public Task<IActionResult> CacheClear()
    {
        _ogcRequest.ClearCache();

        return JsonObject(new { success = true });
    }

    public IActionResult Logout(string ogc_ticket_logout)
    {
        _tokenServices.GetCustomToken(this.Request);

        return Redirect(_ogcRequest.OgcLogoutService(this.Request, _crypto.DecryptTextDefault(ogc_ticket_logout)));
    }
}