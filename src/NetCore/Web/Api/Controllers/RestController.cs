using Api.AppCode.Mvc.Wrapper;
using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Reflection;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Authentication;
using Api.Core.AppCode.Services.Rest;
using E.Standard.Api.App;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.DTOs.Print;
using E.Standard.Api.App.DTOs.Tools;
using E.Standard.Api.App.DTOs.Transformations;
using E.Standard.Api.App.Exceptions;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models;
using E.Standard.Api.App.Reflection;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.DependencyInjection;
using E.Standard.Extensions.Collections;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.Reflection;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.CMS.Extensions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Api.Controllers;

[ApiAuthentication(ApiAuthenticationTypes.Hmac)]
[AppRole(AppRoles.WebgisApi)]
public class RestController : ApiBaseController
{
    private readonly ILogger<RestController> _logger;
    private readonly UrlHelperService _urlHelper;
    private readonly RestServiceFactory _restService;
    private readonly CacheService _cache;
    private readonly ConfigurationService _config;
    private readonly ApiConfigurationService _apiConfig;
    private readonly ApiCookieAuthenticationService _cookies;
    private readonly EtagService _etag;
    private readonly ContentResourceService _contentResource;
    private readonly UploadFilesService _upload;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly ApiLoggingService _apiLogging;
    private readonly IHttpService _http;
    private readonly IRequestContext _requestContext;
    private readonly ICryptoService _crypto;
    private readonly IEnumerable<ICustomApiService> _customServices;
    private readonly IStringLocalizer _stringLocalizer;

    public RestController(ILogger<RestController> logger,
                          UrlHelperService urlHelper,
                          RestServiceFactory restService,
                          CacheService cache,
                          ConfigurationService config,
                          ApiConfigurationService apiConfig,
                          ApiCookieAuthenticationService cookies,
                          EtagService etag,
                          ContentResourceService contentResource,
                          UploadFilesService upload,
                          SubscriberDatabaseService subscriberDb,
                          MapServiceInitializerService mapServiceInitializer,
                          ApiLoggingService apiLogging,
                          IRequestContext requestContext,
                          ICryptoService crypto,
                          IStringLocalizerFactory stringLocalizerFactory,
                          IEnumerable<ICustomApiService> customServices = null)
        : base(logger, urlHelper, requestContext.Http, customServices)
    {
        _logger = logger;
        _urlHelper = urlHelper;
        _restService = restService;
        _cache = cache;
        _config = config;
        _apiConfig = apiConfig;
        _cookies = cookies;
        _etag = etag;
        _contentResource = contentResource;
        _upload = upload;
        _subscriberDb = subscriberDb;
        _mapServiceInitializer = mapServiceInitializer;
        _apiLogging = apiLogging;
        _requestContext = requestContext;
        _http = requestContext.Http;
        _crypto = crypto;
        _stringLocalizer = stringLocalizerFactory.Create(typeof(RestController));
        _customServices = customServices;
    }

    async public Task<IActionResult> Index()
    {
        try
        {
            var ui = this.User.ToUserIdentification(ApiAuthenticationTypes.Hmac, throwExceptions: true);

            return await ApiObject(
                new E.Standard.Api.App.DTOs.IndexDTO()
                {
                    name = "REST",
                    cmsitems = _cache.CmsItems(ui).ToArray(),
                    cms_errormessage = _cache.ErrorMessage,
                    cms_iscorrupt = _cache.IsCorrupt,
                    cache_warnings = _cache.Warnings,
                    folders = new FolderDTO[] {
                        new FolderDTO() {
                            id="services",
                            name="Services"
                        },new FolderDTO {
                            id="extents",
                            name="Extents"
                        }, new FolderDTO {
                            id="srefs",
                            name="Spatial References"
                        }, new FolderDTO {
                            id="tools",
                            name="Tools"
                        }, new FolderDTO {
                            id="search",
                            name="Search"
                        }
                    }
                });
        }
        catch (AuthenticationException)
        {
            return await HandleAuthenticationException();
        }
    }

    public IActionResult Version() => RawResponse(Encoding.UTF8.GetBytes(WebGISVersion.Version.ToString()), "text/plain", null);

    #region Service

    async public Task<IActionResult> Services(bool containerservices = false, string id = "")
    {
        if (!String.IsNullOrWhiteSpace(id))
        {
            return await ServiceInfo(id);
        }

        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var watch = new StopWatch(String.Empty);

            IMapService[] services = _cache.GetServices(ui);

            Array.Sort(services, delegate (IMapService service1, IMapService service2)
            {
                return service1.Name.CompareTo(service2.Name);
            });

            List<ServiceDTO> jsonServices = new List<ServiceDTO>();
            bool onlyContainerServices = containerservices && _cache.HasAddServiceContainers(ui);

            foreach (var service in services)
            {
                string container = onlyContainerServices ?
                     _cache.ServiceContainerName(service.Url, _urlHelper.GetCustomGdiScheme(), ui: ui, useCmsNameIfUnknown: !onlyContainerServices) :
                     (_cache.CmsItemDisplayName(service.Url, ui) ?? (service.Url.Contains("@") ? service.Url.Split('@')[1] : "CMS"));


                if (onlyContainerServices && String.IsNullOrWhiteSpace(container))
                {
                    continue;
                }

                jsonServices.Add(new ServiceDTO()
                {
                    id = service.Url,
                    name = service.Name,
                    type = ServiceDTO.GetServiceType(service).ToString().ToLower(),
                    container = container,
                    supportedCrs = (service is IServiceSupportedCrs ? ((IServiceSupportedCrs)service).SupportedCrs : null),
                    isbasemap = service.IsBaseMap,
                    opacity = service.Opacity,
                    childServices = service is IMapServiceCollection
                        ? service.ResolveCollectionServiceUrls(_cache, ui)
                        : null
                });
            }

            if (onlyContainerServices)
            {
                _cache.SortContainerServices(jsonServices, ui);
            }

            return await ApiObject(watch.Apply<ServicesDTO>(new ServicesDTO()
            {
                services = jsonServices.ToArray()
            }));
        });
    }

    async public Task<IActionResult> ServiceInfo(string ids, string purpose = "")
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var watch = new StopWatch(String.Empty);

            using (var pLog = _apiLogging.UsagePerformaceLogger(this, $"serviceinfo", purpose, ui))
            {
                List<ServiceInfoDTO> infos = new List<ServiceInfoDTO>();
                List<string> unknownServices = new List<string>();
                List<string> unauthorizedServices = new List<string>();
                List<string> exceptions = new List<string>();

                var form = this.Request.FormCollection();

                var restHelper = _restService.Helper;

                if (String.IsNullOrWhiteSpace(ids))
                {
                    throw new ArgumentException("ServiceInfo: parameter ids mising");
                    //ids = String.Join(',', _cache.GetServices(ui).Select(s => s.Url));
                }

                var map = _mapServiceInitializer.Map(_requestContext, ui);

                foreach (string id in ids.Split(','))
                {
                    OrderedDictionary<string, IMapService> services =
                        (await _cache.GetService(id, map, ui, _urlHelper)).ToOrderedDictionaryOrNull(id)
                        ?? (await _mapServiceInitializer.GetCustomServiceByUrlAsync(id, map, ui, this.Request.FormCollection())).ToOrderedDictionaryOrNull(id)
                        ?? (await _restService.Bridge.TryCreateCustomToolService(ui, map, id)).ToOrderedDictionaryOrNull(id)
                        ?? new OrderedDictionary<string, IMapService>() { { id, null } };

                    services = await services.ResolveCollections(_cache, map, ui, _urlHelper);

                    foreach (var service in services)
                    {
                        if (infos.Any(infos => infos.id == service.Key))
                        {
                            // already added (e.g. collection service item)
                            continue;
                        }

                        if (service.Value is null)
                        {
                            var cacheService = await _cache.GetService(service.Key, _mapServiceInitializer.Map(_requestContext, ui), null, _urlHelper);

                            if (cacheService != null)
                            {
                                unauthorizedServices.Add(cacheService.Name);
                            }
                            else
                            {
                                unknownServices.Add(id);
                            }

                            continue;
                        }
                        else if (service.Value is IServiceInitialException serviceInitialException
                            && !String.IsNullOrEmpty(serviceInitialException.InitialException?.ErrorMessage))
                        {
                            exceptions.Add($"{id}: {serviceInitialException.InitialException.ErrorMessage}");

                            continue;
                        }

                        try
                        {
                            ServiceInfoDTO info = restHelper.CreateServiceInfo(this, service.Value, ui);

                            #region Custom Request Parameters (eg. connectionstring for custom user services) 

                            var customRequestParameters = new Dictionary<string, string>();

                            foreach (var customParameter in form.AllKeys.Where(k => k.StartsWith($"custom.{id}.")))
                            {
                                customRequestParameters[customParameter.Substring($"custom.{id}.".Length)] = form[customParameter];
                            }
                            if (customRequestParameters.Count > 0)
                            {
                                info.CustomRequestParameters = customRequestParameters;
                            }

                            #endregion

                            if (info != null)
                            {
                                infos.Add(info);
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add($"{id}: {ex.Message}");
                        }
                    }
                }

                var cmsCopyrightInfos = _cache.CopyrightInfo(infos, ui)?.ToArray();
                //foreach(var info in infos.Where(i=>!String.IsNullOrWhiteSpace(i.copyright))) 
                //{
                //    var cmsCopyrightInfo = cmsCopyrightInfos.Where(c => c.Id == info.copyright).FirstOrDefault();
                //    if(cmsCopyrightInfo!=null)
                //    {
                //        info.CopyrightText += $"{cmsCopyrightInfo.Copyright}\n{cmsCopyrightInfo.Advice}\n{cmsCopyrightInfo.CopyrightLink}";
                //    }
                //}


                return await ApiObject(watch.Apply<ServiceInfosDTO>(new ServiceInfosDTO()
                {
                    services = infos.ToArray(),
                    unknownservices = unknownServices.ToArray(),
                    unauthorizedservices = unauthorizedServices.ToArray(),
                    exceptions = exceptions.ToArray(),
                    copyright = cmsCopyrightInfos
                }));
            }
        });
    }

    async public Task<IActionResult> ServicesQueries()
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var watch = new StopWatch(String.Empty);

            IMapService[] services = _cache.GetServices(ui);

            Array.Sort(services, delegate (IMapService service1, IMapService service2)
            {
                return service1.Name.CompareTo(service2.Name);
            });

            List<ServiceDTO> jsonServices = new List<ServiceDTO>();
            foreach (var service in services)
            {
                var queries = _cache.GetQueries(service.Url, ui);
                if (queries == null || queries.queries == null || queries.queries.Length == 0)
                {
                    continue;
                }

                string container = service.Url.Contains("@") ? service.Url.Split('@')[1] : "CMS";
                jsonServices.Add(new E.Standard.Api.App.DTOs.ServiceDTO()
                {
                    id = service.Url,
                    name = service.Name,
                    type = ServiceDTO.GetServiceType(service).ToString().ToLower(),
                    container = container,
                    supportedCrs = (service is IServiceSupportedCrs ? ((IServiceSupportedCrs)service).SupportedCrs : null),
                    isbasemap = service.IsBaseMap,
                    queries = queries.queries,
                    opacity = service.Opacity
                });
            }

            return await ApiObject(watch.Apply<ServicesDTO>(new ServicesDTO()
            {
                services = jsonServices.ToArray()
            }));
        });
    }

    async public Task<IActionResult> CustomServiceInfo(string url, string displayname, string user, string password)
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            url.CheckAllowedCustomServiceUrl(this.Request, _config);

            var restHelper = _restService.Helper;
            var service = await _mapServiceInitializer.CustomServiceInstanceAsync(_mapServiceInitializer.GenerateNewCustomServiceId(), url, displayname, user, password, ui);
            ServiceInfoDTO info = restHelper.CreateServiceInfo(this, service, ui);

            info.CustomRequestParameters = new
            {
                connection = _mapServiceInitializer.EncodeCustomServiceConnectionString(url, displayname, user, password, ui)
            };

            return await ApiObject(info);
        });
    }

    [Etag(expiraionDays: 7, appendResponseHeaders: false)]
    async public Task<IActionResult> ServiceRequest(string id, string request)
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            switch (request.ToLower())
            {
                case "getmap":
                    return await ApiObject(await _restService.Mapping.PerformGetMap(HttpContext, id, ui));
                case "tile":
                    var data = await _restService.Mapping.PerformTile(HttpContext, id, ui);
                    _etag.AppendEtag(HttpContext, DateTime.Now.AddDays(7));
                    return RawResponse(data, "image/jpg", null);
                case "getselection":
                    return await ApiObject(await _restService.Mapping.PerformGetSelection(HttpContext, id, ui));
                case "getlegend":
                    return await ApiObject(await _restService.Mapping.PerformGetLegend(this, id, ui));
                case "getlegendlayeritem":
                    return await ApiObject(await _restService.Mapping.PerformGetLegendLayerItem(HttpContext, id, ui));
                case "presentations":
                    return await ApiObject(_cache.GetPresentations(await _cache.GetOriginalService(id, ui, _urlHelper), _urlHelper.GetCustomGdiScheme(), ui));
                case "queries":
                    return await ApiObject(_cache.GetQueries(id, ui));
            }
            return await JsonViewSuccess(false, "Unknown request: " + request);
        });
    }

    [ApiAuthentication(ApiAuthenticationTypes.Hmac | ApiAuthenticationTypes.ClientIdAndSecret | ApiAuthenticationTypes.BasicAuthentication)]
    [HttpGet]
    async public Task<IActionResult> ServiceObjectRequest(string serviceId, string request, string objectId, string command = "")
    {
        var httpRequest = this.Request;

        command = !String.IsNullOrWhiteSpace(command) ? command.ToLower() : httpRequest.QueryOrForm("c").ToLower();

        var authTypes = ApiAuthenticationTypes.Hmac;
        if (request.ToLower() == "queries" && (command == "identify" || command == "datalinq_query"))
        {
            authTypes |= ApiAuthenticationTypes.ClientIdAndSecret;
            if (command == "datalinq_query")
            {
                authTypes |= ApiAuthenticationTypes.BasicAuthentication;
            }
        }

        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var watch = new StopWatch(serviceId);

            QueryGeometryType geometryType = QueryGeometryType.Simple;
            if (httpRequest.QueryOrForm("simplifyGeometry") != "false")   // simplifyGeometry noch aus Kompatibilitätsgründen verwenden!!
            {
                geometryType = QueryGeometryType.Full;
            }

            geometryType = httpRequest.QueryOrForm("geometry").ToQueryGeometryType(geometryType);

            switch (request.ToLower())
            {
                case "queries":

                    if (command == "query")
                    {
                        return await _restService.Query.PerformQueryAsync(this, serviceId, objectId,
                                                                          Request.Query.ToCollection(),
                                                                          IsJsonRequest,
                                                                          geometryType,
                                                                          ui);
                    }

                    if (command == "identify")
                    {
                        return await _restService.Query.PerformIdentify(this, serviceId, objectId, Request.Query.ToCollection(), Request.Query["f"] == "json", geometryType, ui, renderFields: Request.Query["renderFields"] != "false");
                    }

                    if (command == "datalinq_query" || command == "integrator_query")
                    {
                        return await _restService.Query.PerformQueryAsync(this, serviceId, objectId, Request.Query.ToCollection(), true, geometryType, ui,
                                                                            renderFields: false,
                                                                            appendHoverShape: false);
                    }

                    if (command == "buffer")
                    {
                        return await _restService.Query.PerformBufferAsync(
                            this,
                            serviceId,
                            objectId,
                            httpRequest.QueryOrForm("source_serviceid"),
                            httpRequest.QueryOrForm("source_queryid"),
                            httpRequest.QueryOrForm("source_oids"),
                            httpRequest.QueryOrForm("distance").ToPlatformDouble(),
                            httpRequest.QueryOrForm("f") == "json", ui, renderFields: httpRequest.QueryOrForm("renderFields") != "false");
                    }
                    if (command == "autocomplete")
                    {
                        return await _restService.Query.PerformAutocompleteQuery(this, serviceId, objectId, Request.Query.ToCollection(), ui);
                    }

                    if (command == "domains")
                    {
                        return await _restService.Query.PerformQueryDomains(this, serviceId, objectId, ui);
                    }

                    var query = await _cache.GetQuery(serviceId, objectId, ui, urlHelper: _urlHelper);

                    if (query == null)
                    {
                        throw new Exception("unknown query");
                    }

                    if (!IsJsonRequest)
                    {
                        return await ApiObject(new QueryFormDTO(new HttpRequestWrapper(Request), query));
                    }

                    query = watch.Apply<QueryDTO>(query);
                    return await ApiObject(query);

                case "edit":
                    return await _restService.Editing.PerformEditServiceRequest(this, serviceId, objectId, command, ui);
            }
            return await JsonViewSuccess(false, "Unknown request: " + request);
        }, authTypes: authTypes);
    }

    [HttpPost]
    async public Task<IActionResult> ServiceObjectPostRequest(string serviceId, string request, string objectId, string command, IFormCollection form)
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var httpRequest = this.Request;

            command = !String.IsNullOrWhiteSpace(command) ? command.ToLower() : httpRequest.QueryOrForm("c").ToLower();

            switch (request.ToLower())
            {
                case "queries":
                    if (String.IsNullOrWhiteSpace(command) || command == "query")
                    {
                        return await _restService.Query.PerformQueryAsync(this, serviceId, objectId,
                                                                          form.ToCollection(),
                                                                          form.CheckBoxRes("__usejson"),
                                                                          form["__geometryType"].ToString().ToQueryGeometryType(),
                                                                          ui,
                                                                          select: true);
                    }
                    else
                    {
                        return await ServiceObjectRequest(serviceId, request, objectId, command);
                    }

                case "edit":
                    return await _restService.Editing.PerformEditServiceRequest(this, serviceId, objectId, command, ui, form.ToCollection());
            }
            return await JsonViewSuccess(false, "Unknown request: " + request);
        });
    }

    // some browsers send an Options request before POST a file to check cors capabilities
    [HttpOptions]
    async public Task<IActionResult> ServiceObjectOptionsRequest(string serviceId, string request, string objectId, string command/*, IFormCollection form*/)
    {
        return await JsonViewSuccess(true);
    }


    #region Edit Service

    public Task<IActionResult> EditServices()
    {
        return SecureMethodHandlerAsync(async (ui) =>
        {
            var watch = new StopWatch(String.Empty);

            IMapService[] services = _cache.GetServices(ui);

            Array.Sort(services, delegate (IMapService service1, IMapService service2)
            {
                return service1.Name.CompareTo(service2.Name);
            });

            List<ServiceDTO> jsonServices = new List<ServiceDTO>();
            foreach (var service in services)
            {
                var editThemes = _cache.GetEditThemes(service.Url, ui).editthemes;
                if (editThemes == null || editThemes.Length == 0)
                {
                    continue;
                }

                if (editThemes.Where(m => m.IsEditServiceTheme).Count() == 0)
                {
                    continue;
                }

                string container = service.Url.Contains("@") ? service.Url.Split('@')[1] : "CMS";
                jsonServices.Add(new ServiceDTO()
                {
                    id = service.Url,
                    name = service.Name,
                    type = ServiceDTO.GetServiceType(service).ToString().ToLower(),
                    container = container,
                    supportedCrs = (service is IServiceSupportedCrs ? ((IServiceSupportedCrs)service).SupportedCrs : null),
                    isbasemap = service.IsBaseMap,
                    opacity = service.Opacity
                });
            }

            return await ApiObject(watch.Apply<ServicesDTO>(new ServicesDTO()
            {
                services = jsonServices.ToArray()
            }));
        });
    }

    public Task<IActionResult> EditThemes()
    {
        return SecureMethodHandlerAsync(async (ui) =>
        {
            var watch = new StopWatch(String.Empty);

            IMapService[] services = _cache.GetServices(ui);

            Array.Sort(services, delegate (IMapService service1, IMapService service2)
            {
                return service1.Name.CompareTo(service2.Name);
            });

            List<object> themes = new List<object>();
            foreach (var service in services)
            {
                var editThemes = _cache.GetEditThemes(service.Url, ui).editthemes;
                if (editThemes == null || editThemes.Length == 0)
                {
                    continue;
                }

                if (editThemes.Where(m => m.IsEditServiceTheme).Count() == 0)
                {
                    continue;
                }

                themes.AddRange(editThemes.Where(m => m.IsEditServiceTheme).Select(m =>
                {
                    return new
                    {
                        id = m.ThemeId,
                        name = service.Name + ": " + m.Name,
                        service = service.Url
                    };
                }).ToArray());
            }

            return await ApiObject(watch.Apply<EditThemesResponseDTO>(new EditThemesResponseDTO()
            {
                themes = themes.ToArray()
            }));
        });
    }

    #endregion

    #endregion

    #region Extents

    async public Task<IActionResult> Extents(string id = "")
    {
        if (!String.IsNullOrWhiteSpace(id))
        {
            return await Extent(id);
        }

        return await SecureMethodHandlerAsync(async (ui) =>
        {
            ExtentDTO[] extents = _cache.GetExtents(ui);

            return await ApiObject(new ExtentsDTO()
            {
                extents = extents
            });
        });
    }

    public Task<IActionResult> Extent(string id)
    {
        return SecureMethodHandlerAsync(async (ui) =>
        {
            ExtentDTO extent = _cache.GetExtent(id, ui);

            return await ApiObject(extent);
        });
    }

    #endregion

    #region Projections

    public Task<IActionResult> SRefs(int id = 0)
    {
        if (id > 0)
        {
            return SRef(id);
        }

        return SecureMethodHandlerAsync(async (ui) =>
        {
            int[] ids = ApiGlobals.SRefStore.SpatialReferences.Ids;

            return await ApiObject(new SRefsDTO()
            {
                ids = ids
            });
        });
    }

    public Task<IActionResult> SRef(int id)
    {
        return SecureMethodHandlerAsync(async (ui) =>
        {
            SpatialReference sref = ApiGlobals.SRefStore.SpatialReferences.ById(id);

            if (sref == null)
            {
                throw new Exception("unknown sref id");
            }

            return await ApiObject(new SRefDTO()
            {
                id = sref.Id,
                name = sref.Name,
                p4 = sref.Proj4,
                axis_x = sref.AxisX.ToString().ToLower(),
                axis_y = sref.AxisY.ToString().ToLower()
            });
        });
    }

    public Task<IActionResult> Project(string proj_arg)
    {
        return SecureMethodHandlerAsync(ui =>
        {
            var arg = JSerializer.Deserialize<ProjectionServiceArgumentDTO>(proj_arg);

            var points = arg.Points;
            using (var transformer = new GeometricTransformerPro(ApiGlobals.SRefStore.SpatialReferences, arg.Srs, arg.ToSrs))
            {
                foreach (var point in points)
                {
                    transformer.Transform(point);
                }
            }

            return JsonObject(new ProjectionServiceResultDTO()
            {
                Srs = arg.ToSrs,
                Points = points.ToArray()
            });
        });
    }

    public Task<IActionResult> Helmert2dTransformation(double lng, double lat)
    {
        return SecureMethodHandlerAsync(ui =>
        {
            Helmert2dDTO helmert2d = null;
            double dist2 = double.MaxValue;
            foreach (var h in _cache.Helmert2dTransformations)
            {
                var d2 = (h.RLng - lng) * (h.RLng - lng) + (h.RLat - lat) * (h.RLat - lat);
                if (d2 < dist2)
                {
                    helmert2d = h;
                    dist2 = d2;
                }
            }

            return JsonObject(helmert2d);
        });
    }

    public Task<IActionResult> Helmert2dTransformations()
    {
        return SecureMethodHandlerAsync(ui =>
        {
            FeatureCollection features = new FeatureCollection();

            int oid = 0;
            foreach (var helmert2d in _cache.Helmert2dTransformations)
            {
                var feature = new E.Standard.WebMapping.Core.Feature();
                feature.Shape = new Point(helmert2d.RLng, helmert2d.RLat);

                feature.GlobalOid = "Helmert2dTransformations." + (++oid);
                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("Name", helmert2d.Name));
                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("Srs", helmert2d.SrsId.ToString()));
                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("Cx", helmert2d.TransX.ToString()));
                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("Cy", helmert2d.TransY.ToString()));
                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("Rx", helmert2d.Rx.ToString()));
                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("Ry", helmert2d.Ry.ToString()));
                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("Rotation", helmert2d.Rotation.ToString()));
                feature.Attributes.Add(new E.Standard.WebMapping.Core.Attribute("Scale", helmert2d.Scale.ToString()));

                features.Add(feature);
            }

            var jsonFeatures = new E.Standard.Api.App.DTOs.FeaturesDTO(features);

            return JsonObject(jsonFeatures);
        });
    }

    #endregion

    #region Tools

    async public Task<IActionResult> Tools(string toolTypes, string client = "")
    {
        return await SecureMethodHandlerAsync(async ui =>
        {
            var apiTools = _cache.GetApiTools(client);
            List<string> toolTypesList = String.IsNullOrWhiteSpace(toolTypes) ? null : new List<string>(toolTypes.ToLower().Replace(" ", "").Split(','));

            List<ToolDTO> tools = new List<ToolDTO>();

            var bridge = _restService.Bridge.CreateInstance(ui);
            var favTools = (await bridge.GetUserFavoriteItemsAsync(E.Standard.WebGIS.Tools.Favorites.Instance, "buttonclick", 8)).ToList();

            var toolHelper = _restService.Tools;

            foreach (var apiTool in apiTools)
            {
                if (toolTypesList == null || toolTypesList.Contains(apiTool.GetType().ToToolId()))
                {
                    ToolDTO tool = toolHelper.Create(apiTool);
                    if (favTools.Contains(tool.id))
                    {
                        tool.FavoritePriority = favTools.IndexOf(tool.id) + 1;
                    }

                    tools.Add(tool);
                }
            }

            return await ApiObject(new ToolCollectionDTO()
            {
                tools = tools.ToArray()
            });
        });
    }

    [HttpPost]
    async public Task<IActionResult> ToolEvent(string toolId, string eventType, string eventString, string toolOptions)
    {
        return await SecureMethodHandlerAsync(async ui =>
        {
            IApiButton button = _cache.GetTool(toolId)
                .ThrowIfNull(() => $"Unknown Tool :{toolId}")
                .CheckToolPolicy(ui);

            var bridge = _restService.Bridge.CreateInstance(ui, button);

            ApiEventResponse apiResponse = null;

            ApiToolEventArguments e = _restService.Tools.CreateApiToolEventArguments(button, eventString, toolOptions);

            #region Files

            if (_upload.HasFiles(this.Request))
            {
                var files = _upload.GetFiles(this.Request);
                foreach (string name in files.Keys)
                {
                    var file = files[name];
                    byte[] buffer = file.Data;

                    e.AddFile(name, new ApiToolEventArguments.ApiToolEventFile()
                    {
                        Data = buffer,
                        FileName = file.FileName.ToFilename(),
                        ContentType = file.ContentType
                    });
                }
            }

            #endregion

            bridge.CurrentEventArguments = e;

            var dependencyProvider = new ToolDependencyProvider(bridge, e, _stringLocalizer);

            var advancedToolProperties = button.GetType().GetCustomAttribute<AdvancedToolPropertiesAttribute>();
            if (advancedToolProperties != null)
            {
                if (advancedToolProperties.AnonymousUserIdDependent == true && !String.IsNullOrWhiteSpace(e["_anonymous_userid"]))
                {
                    bridge.SetAnonymousUserGuid(bridge.AnonymousUserGuid(e["_anonymous_userid"]));
                }
            }

            var eventMetadata = Request?.QueryOrForm("eventMeta").ToEventMetadata(ui?.Username);
            string loggerEventType = $"{eventType}{(!String.IsNullOrEmpty(e["_method"]) ? "." + e["_method"] : String.Empty)}";

            if (!String.IsNullOrEmpty(eventMetadata?.Portal))
            {
                await _customServices.LogToolRequest(eventMetadata.Portal, eventMetadata.Category, eventMetadata.MapName, $"{button?.GetType().ToToolId() ?? String.Empty} => {loggerEventType}", ui?.Username);
            }

            using (var pLog = _apiLogging.UsagePerformaceLogger(this, button, loggerEventType, ui))
            {
                if (eventType == "buttonclick")
                {
                    apiResponse = button switch
                    {
                        { } when button.GetType().ImplementsAnyInterface(
                                typeof(IApiServerButton),
                                typeof(IApiServerTool),
                                typeof(IApiServerToolLocalizable<>),
                                typeof(IApiClientTool)
                            ) =>
                             Invoker.Invoke<ApiEventResponse>(button, "OnButtonClick", dependencyProvider),
                        { } when button.GetType().ImplementsAnyInterface(
                                typeof(IApiServerToolAsync),
                                typeof(IApiServerButtonAsync),
                                typeof(IApiServerToolLocalizableAsync<>)
                            ) =>
                            await Invoker.InvokeAsync<ApiEventResponse>(button, "OnButtonClick", dependencyProvider),

                        _ => null
                    };

                    // garbage
                    /*
                    if (button is IApiServerButton serverButton)
                    {
                        apiResponse = serverButton.OnButtonClick(bridge, e);
                    }
                    else if (button is IApiServerTool serverTool)
                    {
                        apiResponse = serverTool.OnButtonClick(bridge, e);
                    }
                    else if (button is IApiClientTool clientTool)
                    {
                        apiResponse = clientTool.OnButtonClick(bridge, e);
                    }
                    else if (button is IApiServerToolAsync serverToolAsync)
                    {
                        apiResponse = await serverToolAsync.OnButtonClick(bridge, e);
                    }
                    else if (button is IApiServerButtonAsync serverButtonAsync)
                    {
                        apiResponse = await serverButtonAsync.OnButtonClick(bridge, e);
                    }
                    */

                    if (button != null && e.AsDefaultTool == false)
                    {
                        await bridge.SetUserFavoritesItemAsync(E.Standard.WebGIS.Tools.Favorites.Instance, "buttonclick", button.GetType().ToToolId());
                    }

                }
                else if (eventType == "toolevent")
                {
                    apiResponse = button switch
                    {
                        { } when button.GetType().ImplementsAnyInterface(
                                typeof(IApiServerTool)
                            )
                            => Invoker.Invoke<ApiEventResponse>(button, "OnEvent", dependencyProvider),
                        { } when button.GetType().ImplementsAnyInterface(
                                typeof(IApiServerToolAsync),
                                typeof(IApiServerToolLocalizableAsync<>)
                            )
                            => await Invoker.InvokeAsync<ApiEventResponse>(button, "OnEvent", dependencyProvider),
                        _ => null
                    };

                    // garbage

                    //if (button is IApiServerTool)
                    //{
                    //    apiResponse = ((IApiServerTool)button).OnEvent(bridge, e);
                    //}
                    //else if (button is IApiServerToolAsync)
                    //{
                    //    apiResponse = await ((IApiServerToolAsync)button).OnEvent(bridge, e);
                    //}
                }
                else if (eventType == "servertoolcommand" && e["_method"] != null)
                {
                    var serverCommandInstance = button.ServerCommandInstance(bridge, e);

                    foreach (string command in e["_method"].Split(','))
                    {
                        if (command.StartsWith("_event_handler_"))
                        {
                            #region Event Handler

                            ServerEventHandlers handler = (ServerEventHandlers)Enum.Parse(typeof(ServerEventHandlers), command.Substring("_event_handler_".Length), true);
                            foreach (var methodInfo in serverCommandInstance.GetType().GetMethods())
                            {
                                ServerEventHandlerAttribute[] attributes = (ServerEventHandlerAttribute[])methodInfo.GetCustomAttributes(typeof(ServerEventHandlerAttribute), true);
                                if (attributes == null || attributes.Length == 0 || attributes.Where(a => a.Handler == handler).FirstOrDefault() == null)
                                {
                                    continue;
                                }

                                try
                                {
                                    apiResponse = await _restService.Tools.InvokeMethodAsync<ApiEventResponse>(methodInfo, serverCommandInstance, dependencyProvider);
                                }
                                catch (System.Reflection.TargetInvocationException ex)
                                {
                                    if (ex.InnerException != null)
                                    {
                                        throw ex.InnerException;
                                    }

                                    throw;
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            #region Server Comannd

                            string methodName = command;
                            if (command.Contains("[") && command.EndsWith("]"))
                            {
                                var pos = command.IndexOf("[");
                                string commandIndexValue = command.Substring(pos + 1, command.Length - pos - 2);
                                e["_commandIndexValue"] = commandIndexValue;
                                methodName = command.Substring(0, pos);
                            }

                            foreach (var methodInfo in serverCommandInstance.GetType().GetMethods())
                            {
                                ServerToolCommandAttribute[] attributes = (ServerToolCommandAttribute[])methodInfo.GetCustomAttributes(typeof(ServerToolCommandAttribute), true);
                                if (attributes == null || attributes.Length == 0)
                                {
                                    continue;
                                }

                                foreach (var attribute in attributes)
                                {
                                    if (methodName == attribute.Method)
                                    {
                                        try
                                        {
                                            //apiResponse = (ApiEventResponse)methodInfo.Invoke(button, new object[] { bridge, e });
                                            apiResponse = await _restService.Tools.InvokeMethodAsync<ApiEventResponse>(methodInfo, serverCommandInstance, dependencyProvider);
                                        }
                                        catch (System.Reflection.TargetInvocationException ex)
                                        {
                                            if (ex.InnerException != null)
                                            {
                                                throw ex.InnerException;
                                            }

                                            throw;
                                        }
                                    }
                                }
                            }

                            #endregion
                        }
                    }
                }
            }
            if (apiResponse != null)
            {
                if (button is IApiPostRequestEvent)
                {
                    apiResponse = await ((IApiPostRequestEvent)button).PostProcessEventResponseAsync(bridge, e, apiResponse);
                }

                return await _restService.Tools.ToolResponseResult(this, apiResponse, ui);
            }

            return await JsonViewSuccess(true);
        });
    }

    //[ValidateInput(false)]
    [ApiAuthentication(ApiAuthenticationTypes.Hmac | ApiAuthenticationTypes.PortalProxyRequest)]
    async public Task<IActionResult> ToolMethod(string toolId, string method)
    {
        return await SecureMethodHandlerAsync(async ui =>
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("ToolMethod {toolId} {method}", toolId, method);
            }

            IApiButton button = _cache.GetTool(toolId);

            if (button == null)
            {
                throw new Exception($"Unknown tool {toolId}");
            }

            var bridge = _restService.Bridge.CreateInstance(ui, button);

            ApiEventResponse apiResponse = null;

            ApiToolEventArguments e = new ApiToolEventArguments(bridge,
                                                                Request.FormOrQueryParameters(),
                                                                new string[] { "toolid", "method" },
                                                                configuration: button.ToolConfiguration(_config));
            bridge.CurrentEventArguments = e;

            var dependencyProvider = new ToolDependencyProvider(bridge, e, _stringLocalizer);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("ToolMethod Arguments: {arguments}", e.RawEventString);
            }

            var restToolHelper = _restService.Tools;
            var serverCommandInstance = button.ServerCommandInstance(bridge, e);

            foreach (var methodInfo in serverCommandInstance.GetType().GetMethods())
            {
                ServerToolCommandAttribute[] attributes = (ServerToolCommandAttribute[])methodInfo.GetCustomAttributes(typeof(ServerToolCommandAttribute), true);
                if (attributes == null || attributes.Length == 0)
                {
                    continue;
                }

                foreach (var attribute in attributes)
                {
                    if (method == attribute.Method)
                    {
                        apiResponse = await restToolHelper.InvokeMethodAsync<ApiEventResponse>(methodInfo, serverCommandInstance, dependencyProvider);
                    }
                }
            }

            if (apiResponse != null)
            {
                if (button is IApiPostRequestEvent)
                {
                    apiResponse = await ((IApiPostRequestEvent)button).PostProcessEventResponseAsync(bridge, e, apiResponse);
                }

                return await restToolHelper.ToolResponseResult(this, apiResponse, ui);
            }

            return await JsonViewSuccess(true);
        }, authTypes: ApiAuthenticationTypes.Hmac | ApiAuthenticationTypes.PortalProxyRequest);
    }

    [ApiAuthentication(ApiAuthenticationTypes.Hmac | ApiAuthenticationTypes.PortalProxyRequest)]
    [Etag(expiraionDays: 7, appendResponseHeaders: false)]
    [HttpGet]
    public Task<IActionResult> ToolData(string toolId, string method)
    {
        return ToolMethod(toolId, method);
    }

    [HttpPost]
    async public Task<IActionResult> ToolUndo(string toolId, string undo)
    {
        return await SecureMethodHandlerAsync(async ui =>
        {
            IApiButton button = _cache.GetTool(toolId);
            if (!(button is IApiUndoTool))
            {
                throw new Exception("Tool is not an undo tool");
            }

            var bridge = _restService.Bridge.CreateInstance(ui, button);

            var toolUndo = JSerializer.Deserialize<ToolUndoDTO>(undo);

            var apiResponse = await ((IApiUndoTool)button).PerformUndo(bridge, toolUndo);

            if (apiResponse != null)
            {
                return await _restService.Tools.ToolResponseResult(this, apiResponse, ui);
            }

            return await JsonViewSuccess(true);
        });
    }

    [HttpGet]
    [Etag(365)]
    public IActionResult ToolResource(string id)
    {
        byte[] resourceBytes = null;
        string contentType = String.Empty;

        if (!String.IsNullOrWhiteSpace(Request.Query["company"]))
        {
            resourceBytes = _contentResource.GetImageBytes(Request, id + "~png", String.IsNullOrWhiteSpace(Request.Query["sub"]) ? "tools" : Request.Query["sub"].ToString());
            if (resourceBytes != null)
            {
                return RawResponse(resourceBytes, "image/png", null);
            }
        }

        CacheInstance.ToolResource toolResource = _cache.GetToolResource(id);

        if (toolResource != null)
        {
            resourceBytes = toolResource.ResourceBytes;
            contentType = toolResource.ContentType;
        }

        return RawResponse(resourceBytes ?? new byte[0], contentType, null);
    }

    [Etag(365)]
    public IActionResult ImageResource(string id, string sub = "tools")
    {
        byte[] resourceBytes = _contentResource.GetImageBytes(Request, id, sub);

        if (resourceBytes == null)
        {
            return null;
        }

        return RawResponse(resourceBytes, "image/png", null);
    }

    #endregion

    #region Marker Images

    [Etag(365)]
    public IActionResult UserMarkerImage(string id, int width = 30, int height = 0)
    {
        if (height == 0)
        {
            height = (int)(width * 1.2f);
        }

        return RawResponse(
            _restService.Imaging.GetUserMarkerImageBytes(id, width, height),
            "image/png",
            null);
    }

    [Etag(365)]
    public IActionResult NumberMarker(string id, string style, string c, int? w, int? h, float? p, float? fs, string hc)
    {
        switch (style)
        {
            case "coords":
                return RawResponse(
                         _restService.Imaging.GetCoordsMarkerImageBytes(id.TryParseToInt32(null), w: w, h: h, colorsHex: String.IsNullOrEmpty(c) ? null : c.Split(',')),
                        "image/png",
                        null);
            case "chainage":
                return RawResponse(
                         _restService.Imaging.GetChainageMarkerImageBytes(id.TryParseToInt32(null), w: w, h: h, colorsHex: String.IsNullOrEmpty(c) ? null : c.Split(',')),
                        "image/png",
                        null);
            default:
                if (id == "_sprite")
                {
                    return RawResponse(
                        _restService.Imaging.GetQueryMarkerSpriteBytes(
                            width: w, height: h,
                            colorsHex: String.IsNullOrEmpty(hc) ?   // hc ... Hightlight Color => overrules original color!
                                (String.IsNullOrEmpty(c) ? null : c.Split(',')) :
                                hc.Split(','),
                            fontSize: fs.HasValue ? fs.Value : null,
                            penWidth: p.HasValue ? p.Value : 1.5f),
                        "image/png",
                        null);
                }
                else
                {
                    return RawResponse(
                        _restService.Imaging.GetQueryMarkerImageBytes(
                            id.TryParseToInt32(null),
                            width: w, height: h,
                            colorsHex: String.IsNullOrEmpty(hc) ?   // hc ... Hightlight Color => overrules original color!
                                (String.IsNullOrEmpty(c) ? null : c.Split(',')) :
                                hc.Split(','),
                            fontSize: fs.HasValue ? fs.Value : null,
                            penWidth: p.HasValue ? p.Value : 1.5f),
                        "image/png",
                        null);
                }
        }
    }

    public IActionResult TextMarker(string id, string c, int? w, int? h, float? p, float? fs, string hc)
    {

        return RawResponse(
            _restService.Imaging.GetQueryMarkerImageBytes(
                id,
                width: w, height: h,
                colorsHex: String.IsNullOrEmpty(hc) ?   // hc ... Hightlight Color => overrules original color!
                    (String.IsNullOrEmpty(c) ? null : c.Split(',')) :
                    hc.Split(','),
                fontSize: fs.HasValue ? fs.Value : null,
                penWidth: p.HasValue ? p.Value : 1.5f),
            "image/png",
            null);
    }

    #endregion

    #region Search

    async public Task<IActionResult> Search(string id = "")
    {
        if (!String.IsNullOrWhiteSpace(id))
        {
            return await SearchService(id);
        }

        return await SecureMethodHandlerAsync(async (ui) =>
        {
            List<FolderDTO> folders = new List<FolderDTO>();

            foreach (ISearchService service in _cache.GetSearchServices(ui))
            {
                folders.Add(new FolderDTO()
                {
                    id = service.Id,
                    name = service.Name + " (" + service.Id + ")"
                });
            }

            var customSearchServices = _customServices.CustomSearchServices();
            foreach (var key in customSearchServices.Keys)
            {
                folders.Add(new FolderDTO()
                {
                    id = key,
                    name = customSearchServices[key]
                });
            }

            return await ApiObject(
                new E.Standard.Api.App.DTOs.IndexDTO()
                {
                    name = "SEARCH",
                    folders = folders.ToArray()
                });
        });
    }

    #region Search Service

    async public Task<IActionResult> SearchService(string serviceId)
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            List<ISearchService> services = new List<ISearchService>();
            foreach (string id in serviceId.Split(','))
            {
                ISearchService service = _cache.GetSearchService(id, ui);

                if (service != null)
                {
                    services.Add(service);
                }
            }
            if (services.Count == 0)
            {
                throw new Exception("Unknown search service");
            }

            string command = Request.Query["c"];
            if (command == "query")
            {
                return await _restService.Search.PerformSearchServiceAsync(this, services, Request.Query.ToCollection(), ui);
            }
            else if (command == "original")
            {
                return await _restService.Search.PerformSearchServiceItemOriginal(this, services, Request.Query.ToCollection(), ui);
            }
            else if (command == "meta")
            {
                return await _restService.Search.PerformSearchServiceMetaAsync(this, services, ui);
            }
            else if (command == "meta_geocodes")
            {
                return await _restService.Search.PerformSearchServiceMetaGeoCodesAsync(this, services, ui);
            }
            else if (command == "item_meta")
            {
                return await _restService.Search.PerformSearchServiceItemMetaAsync(this, services, Request.Query.ToCollection(), ui);
            }

            return await ApiObject(new SearchServiceFormDTO(services[0],
                services[0] is ISearchService2 ? await ((ISearchService2)services[0]).TypesAsync(_http) : null)
            {
                Action = HtmlHelper.RequestUrl(new HttpRequestWrapper(this.Request), HtmlHelper.UrlSchemaType.Remove)
            });
        });
    }

    [HttpPost]
    async public Task<IActionResult> SearchService(string serviceId, IFormCollection form)
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            List<ISearchService> services = new List<ISearchService>();
            foreach (string id in serviceId.Split(','))
            {
                ISearchService service = _cache.GetSearchService(id, ui);

                if (service != null)
                {
                    services.Add(service);
                }
            }
            if (services.Count == 0)
            {
                throw new Exception("Unknown search service");
            }

            return await _restService.Search.PerformSearchServiceAsync(this, services, form.ToCollection(), ui);
        });
    }

    #endregion

    #region GeoReferencing

    async public Task<IActionResult> GeoReference(string term)
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var bridge = _restService.Bridge.CreateInstance(ui);

            return await JsonObject(await bridge.GeoReferenceAsync(term, Request.Query["services"], Request.Query["categories"]));
        });
    }

    #endregion

    #endregion

    #region Json

    async public Task<IActionResult> GetJsonTemplate(string name)
    {
        try
        {
            string json = System.IO.File.ReadAllText($"{_urlHelper.AppRootPath()}/json-templates/{name}.js");
            return JsonView(json);
        }
        catch (Exception ex)
        {
            _mapServiceInitializer.LogException(_requestContext, ex, "getjsontemplate");

            if (ex is System.IO.FileNotFoundException || ex is System.IO.DirectoryNotFoundException)
            {
                return await ThrowJsonException(new Exception("Templete-file not found: " + name));
            }

            return await ThrowJsonException(ex);
        }
    }

    async public Task<IActionResult> Sleep(int milliseconds)
    {
        await Task.Delay(milliseconds);
        return await JsonObject(new { success = true });
    }

    #endregion

    #region Dynamic Content

    [HttpPost]
    async public Task<IActionResult> DynamicContentProxy()
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            string type = Request.DynamicContentType();
            string url = Request.DynamicContentUrl(ui);

            if (type == "api-query" || type == "apiquery")
            {
                string[] rawUrlParts = url.Split('?')[0].Split('/');

                string queryid = rawUrlParts[rawUrlParts.Length - (rawUrlParts.Last().ToLower() == "query" ? 2 : 1)];
                string serviceId = rawUrlParts[rawUrlParts.Length - (rawUrlParts.Last().ToLower() == "query" ? 4 : 3)];

                NameValueCollection queryString = null;
                if (url.Contains("?"))
                {
                    string parameters = url.Substring(url.IndexOf("?"), url.Length - url.IndexOf("?"));
                    queryString = HttpUtility.ParseQueryString(parameters);

                    foreach (string key in Request.Query.Keys)
                    {
                        if (String.IsNullOrEmpty(queryString[key]))
                        {
                            queryString.Add(key, Request.Query[key]);
                        }
                    }
                }

                return await _restService.Query.PerformQueryAsync(this, serviceId, queryid, queryString, true, QueryGeometryType.Simple, ui);
            }
            else if (type == "georss")
            {
                var features = await FeaturesDTO.FromGeoRSS(_http, url);

                return await JsonObject(features);
            }
            else if (type == "geojson")
            {
                try
                {
                    byte[] data = await _http.GetDataAsync(url);  //dummyConnector.GetBytes(url);


                    return RawResponse(data, "application/json", null);
                }
                catch (Exception ex)
                {
                    throw ex.InnerException ?? ex;
                }
            }
            else if (type == "geojson-embedded")
            {
                try
                {
                    string dataString = await _http.GetStringAsync(url);

                    var geoJsonResponse = JSerializer.Deserialize<GeoJsonResponseDTO>(dataString);

                    return RawResponse(Encoding.UTF8.GetBytes(geoJsonResponse.response?.ToString() ?? String.Empty), "application/json", null);
                }
                catch (Exception ex)
                {
                    throw ex.InnerException ?? ex;
                }
            }

            return null;
        });
    }

    #endregion

    #region Print / Download Map Image

    [HttpPost]
    async public Task<IActionResult> Print()
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var eventMetadata = Request?.QueryOrForm("eventMeta").ToEventMetadata(ui?.Username);
            if (!String.IsNullOrEmpty(eventMetadata?.Portal))
            {
                var layoutId = Request.FormOrQuery("layout");
                var layout = _cache.GetPrintLayouts(_config.GdiSchemeDefault(), ui).Where(l => l.Id == layoutId).FirstOrDefault();

                await _customServices.LogToolRequest(eventMetadata.Portal,
                                                     eventMetadata.Category,
                                                     eventMetadata.MapName,
                                                     $"print => {(layout != null ? layout.Name : String.Empty)} => {Request.QueryOrForm("format")} => 1:{Request.QueryOrForm("scale")}",
                                                     ui?.Username);
            }

            using (var pLog = _apiLogging.UsagePerformaceLogger(this, $"print", "", ui))
            {
                return await _restService.Print.PerformPrintAsync(this, ui);
            }
        });
    }

    async public Task<IActionResult> PrintLayouts()
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var layouts = _cache.GetPrintLayouts(_urlHelper.GetCustomGdiScheme(), ui);

            return await ApiObject(layouts);
        });
    }

    [HttpPost]
    async public Task<IActionResult> DownloadMapImage()
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            using (var pLog = _apiLogging.UsagePerformaceLogger(this, $"downloadmapimage", null, ui))
            {
                return await _restService.Print.PerformDownloadMapImageAsync(this, ui);
            }
        });
    }

    [HttpPost]
    [ApiAuthentication(ApiAuthenticationTypes.Hmac | ApiAuthenticationTypes.ClientIdAndSecret)]
    async public Task<IActionResult> PlotService()
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            var result = await _restService.Print.PerformPlotServiceRequestAsync(this, ui);

            return result;
        },
        ApiAuthenticationTypes.Hmac | ApiAuthenticationTypes.ClientIdAndSecret);
    }

    #endregion

    #region Export Features

    async public Task<IActionResult> ExportFeatures(string serviceId, string queryId, string featureIds, string queryFeatures, string format)
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            string name = String.Empty, exportText = String.Empty, fileTitle = String.Empty;
            string fileExtension = "csv";

            if (!String.IsNullOrEmpty(serviceId) && !String.IsNullOrEmpty(queryId))
            {
                var query = await _cache.GetQuery(serviceId, queryId, ui, urlHelper: _urlHelper);
                if (query == null)
                {
                    throw new Exception("Query not found");
                }

                var oids = featureIds.Split(',').Select(id => long.Parse(id)).ToArray();
                var filter = new E.Standard.WebMapping.Core.Api.Bridge.ApiOidsFilter(oids);
                filter.QueryGeometry = false;

                var engine = new QueryEngine();
                if (await engine.PerformAsync(_requestContext, query, filter, advancedQueryMethod: QueryEngine.AdvancedQueryMethod.Normal))
                {
                    var tableExportFormat = query.TableExportFormats?
                                                 .Where(f => f.Id == format)
                                                 .FirstOrDefault();


                    fileTitle = $"{queryId}_{Guid.NewGuid():N}";

                    FeatureCollection features = await _restService.Helper.PrepareFeatureCollection(engine.Features, query, null, null, renderFields: tableExportFormat == null);
                    features.OrderByIds(oids);

                    if (tableExportFormat != null)
                    {
                        fileExtension = tableExportFormat.FileExtension;
                        name = $"{tableExportFormat.Name}.{fileExtension}";

                        exportText = features.ToPattern(tableExportFormat.FormatString);
                    }
                    else
                    {
                        switch (format)
                        {
                            case "_csv":
                            case "_csv_excel":
                            default:
                                exportText = features.ToCsv(excel: format == "_csv_excel");
                                name = query.Name + ".csv";
                                break;
                        }
                    }
                }
            }
            else if (!String.IsNullOrEmpty(queryFeatures))
            {
                fileTitle = $"{Guid.NewGuid():N}";

                var featureCollection = JSerializer.Deserialize<QueryFeaturesDTO>(queryFeatures);

                switch (format)
                {
                    case "_csv":
                    case "_csv_excel":
                    default:
                        exportText = featureCollection.ToCsv(excel: format == "_csv_excel");
                        name = "table.csv";
                        break;
                }
            }

            string fileName = $"{fileTitle}.{fileExtension}";
            System.IO.File.WriteAllText($"{_urlHelper.OutputPath()}/{fileName}".ToPlatformPath(), exportText, _config.DefaultTextDownloadEncoding());

            if (String.IsNullOrWhiteSpace(fileName))
            {
                throw new Exception("Unknown error");
            }

            return await JsonObject(new
            {
                success = true,
                downloadid = _crypto.EncryptTextDefault(fileName, CryptoResultStringType.Hex),
                name = name
            });
        });
    }

    #endregion

    #region Snapping

    [HttpPost]
    async public Task<IActionResult> Snapping()
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            return await _restService.Snapping.PerformSnappingAsync(this, ui);
        });
    }

    #endregion

    #region Download

    async public Task<IActionResult> Download(string id, string n = "", string contentType = "application/octet-stream")
    {
        return await SecureMethodHandlerAsync(async (ui) =>
        {
            string fileName = _crypto.DecryptTextDefault(id);

            fileName = fileName.Replace("\\", "/");

            if (fileName.Contains(@"/../"))
            {
                throw new IOException("Not allowed");
            }

            string filePath = $"{_urlHelper.OutputPath()}/{fileName}";

            // ToDo: Wird nicht in der Cloud funktionieren, weil es da keine Output Verzeichnis gibt...
            if (fileName.ToLower().EndsWith(".pdf"))
            {
                string clientFileName = "webgis-map_" + DateTime.Now.ToShortDateString() + "_" + DateTime.Now.ToLongTimeString() + ".pdf";
                var nvc = new NameValueCollection();
                nvc.Add("content-disposition", $"attachment; filename=\"{System.Net.WebUtility.UrlEncode(clientFileName)}\"");
                return RawResponse((await filePath.BytesFromUri(_http))?.ToArray(), "application/pdf", nvc);
            }
            else /*if (n.ToLower().EndsWith(".gpx") || fileName.ToLower().EndsWith(".gpx") ||
                       n.ToLower().EndsWith(".csv") || fileName.ToLower().EndsWith(".csv") ||
                       n.ToLower().EndsWith(".json") || fileName.ToLower().EndsWith(".json") ||
                       n.ToLower().EndsWith(".zip") || fileName.ToLower().EndsWith(".zip"))*/
            {
                string clientFileName = n;
                var nvc = new NameValueCollection();

                if (contentType == "application/octet-stream")
                {
                    nvc.Add("content-disposition", $"attachment; filename=\"{System.Net.WebUtility.UrlEncode(clientFileName)}\"");
                }
                var data = await filePath.BytesFromUri(_http); // System.IO.File.ReadAllBytes(filePath);

                if (!(n ?? String.Empty).StartsWith("print_"))  // Ausdruch kann über die Druckvorschau öfter gedruckt werden.
                {
                    filePath.TryDelete();
                }

                return RawResponse(data.ToArray(), contentType, nvc);
            }
        });
    }

    #endregion

    #region Authentication (ClientId & ClientSecret)

    public IActionResult Login()
    {
        return ViewResult(new RestLoginModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(RestLoginModel login)
    {
        try
        {
            var db = _subscriberDb.CreateInstance();

            var client = db.GetClientByClientId(login.ClientId);
            if (client == null || !client.IsClientSecret(login.ClientSecret))
            {
                throw new Exception("Invalid ClientId or ClientSecret");
            }

            var subscriber = db.GetSubscriberById(client.Subscriber);
            if (subscriber == null)
            {
                throw new Exception("Unknown subscriber!?");
            }

            _cookies.SetAuthCookie(HttpContext, $"clientid:{client.ClientId}:{subscriber.Name}@{client.ClientName}");

            return RedirectToActionResult("Index");
        }
        catch (Exception ex)
        {
            _mapServiceInitializer.LogException(_requestContext, ex, "login");
            login.ErrorMessage = ex.Message;
            return ViewResult(login);
        }
    }

    public IActionResult Logout()
    {
        _cookies.SignOut(HttpContext);
        return RedirectToActionResult("Index");
    }

    public Task<IActionResult> RequestHmac(string clientid)
    {
        return _restService.RequestHmac.RequestHmac(this, clientid);
    }

    public Task<IActionResult> AllCmsUserRoles(bool groupBy = false)
    {
        if (!_crypto.VerifyCustomPassword((int)CustomPasswords.ApiAdminQueryPassword, Request.Query["pwd"]))
        {
            return JsonObject(new string[0]);
        }

        if (groupBy)
        {
            return JsonObject(_cache.AllUserRoles);
        }
        else
        {
            return JsonObject(_cache.AllCmsUserRoles());
        }
    }

    public Task<IActionResult> ClientInfo()
    {
        return JsonObject(new
        {
            windowsAuthEndPoint = $"{_urlHelper.PortalUrl(HttpSchema.Https)}/hmac"
        });
    }

    #endregion

    #region Branches

    public Task<IActionResult> Branches()
    {
        if (!_apiConfig.AllowBranches)
        {
            return base.JsonObject(Array.Empty<string>());
        }

        var branches = new HashSet<string>() { "" };

        _config.GetPathsStartWith(ApiConfigKeys.ToKey("cmspath"))
            .Where(k => k.Contains("$"))
            .ToList()
            .ForEach(k => branches.Add(k.Split('$').Last()));

        return base.JsonObject(branches);
    }

    #endregion

    #region Method Handler

    async private Task<IActionResult> SecureMethodHandlerAsync(Func<CmsDocument.UserIdentification, Task<IActionResult>> func, ApiAuthenticationTypes authTypes = ApiAuthenticationTypes.Hmac)
    {
        CmsDocument.UserIdentification ui = null;

        try
        {
            ui = this.User.ToUserIdentification(authTypes, throwExceptions: true);

            return await func(ui);
        }
        catch (AuthenticationException)
        {
            return await HandleAuthenticationException();
        }
        catch (ReportExceptionException ree)
        {
            _logger.LogError(ree.Message);

            _apiLogging.LogReportException(ree, ui);
            _mapServiceInitializer.LogException(_requestContext, ree, $"{CurrentControllerName}.{CurrentActionName}");

            return await ThrowJsonException(ree);
        }
        catch (ReportWarningException rwe)
        {
            _logger.LogWarning(rwe.Message);

            _apiLogging.LogReportException(rwe, ui);
            return await ThrowJsonException(rwe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            _mapServiceInitializer.LogException(_requestContext, ex, $"{CurrentControllerName}.{CurrentActionName}",
                service: Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(this.Request));

            return await ThrowJsonException(ex);
        }
    }

    #endregion
}