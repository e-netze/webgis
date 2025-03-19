using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services.Cms;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.ThreadsafeClasses;
using E.Standard.WebGIS.Api.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Services.Cache;

public class CacheService
{
    #region Declarations

    #region Cache Properties

    private readonly ConcurrentDictionary<string, object> _generalCache = new ConcurrentDictionary<string, object>();

    private static readonly object _lockThis = new object();

    private bool _isCorrupt = false;
    private bool _isInitialized = false;

    internal ConcurrentDictionary<string, string[]> _allUserRoles = new ConcurrentDictionary<string, string[]>();

    private CacheInstance _cacheInstance = null;

    #endregion

    #region Dependency Injection

    private readonly ILogger<CacheService> _logger;
    private readonly ConfigurationService _config;
    private readonly CmsDocumentsService _cmsDocuments;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<ICustomCacheService> _customCacheServices;
    private readonly IWarningsLogger _warningsLogger;

    #endregion

    #endregion

    public CacheService(ILogger<CacheService> logger,
                        IServiceProvider serviceProvider,
                        ConfigurationService config,
                        CmsDocumentsService cmsDocuments,
                        IWarningsLogger warningsLogger,
                        IEnumerable<ICustomCacheService> customCacheServices = null)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config;
        _cmsDocuments = cmsDocuments;
        _warningsLogger = warningsLogger;
        _customCacheServices = customCacheServices;
    }

    public void Init(IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders)
    {
        lock (_lockThis)
        {
            if (_isInitialized)
            {
                return;
            }

            ErrorMessage = String.Empty;

            try
            {
                this.GdiSchemeDefault = _config.GdiSchemeDefault();

                var cacheInstance = new CacheInstance(this);
                cacheInstance.Init();

                var useNewCacheInstance = true;
                if (_cacheInstance != null && _cacheInstance.IsCorrupt == false)  // wenn es schon eine cacheInstance gibt, die nicht korrupt ist, vorher die neue überprüfen
                {
                    if (cacheInstance.IsCorrupt)
                    {
                        useNewCacheInstance = false;
                        LastInitErrorMessage = cacheInstance.ErrorMessage;
                    }
                }

                if (useNewCacheInstance)
                {
                    //using (var mutex = await FuzzyMutexAsync.LockAsync("refreshCacheService"))
                    {
                        var currentCacheInstance = _cacheInstance;
                        _cacheInstance = cacheInstance;

                        if (currentCacheInstance != null)
                        {
                            Task.Run(async () =>
                            {
                                await Task.Delay(5000);
                                try
                                {
                                    currentCacheInstance.Clear();

                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                    GC.Collect();
                                }
                                catch { }
                            });
                        }
                        else
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                        }


                        _isInitialized = true;

                        // Instance kann korrupt sein => wird beim Start trotzdem einmal verwendet, weil ja nicht alle CmsItems korrupt sein müssen (besser als nix prinzip)
                        // Beim einem Cache Clear wird dann oben überprüft
                        _isCorrupt = _cacheInstance.IsCorrupt;
                        LastInitErrorMessage = ErrorMessage = _cacheInstance.ErrorMessage;
                    }

                    //return useNewCacheInstance;
                }
            }
            catch (Exception ex)
            {
                _isCorrupt = _cacheInstance == null || _cacheInstance.IsCorrupt == false;
                ErrorMessage = ex.Message + (ex is NullReferenceException ? $"{System.Environment.NewLine} {ex.StackTrace}" : String.Empty);
            }

            #region Datalinq, etc

            foreach (var expectableUserRoleNamesProvider in expectableUserRolesNamesProviders ?? [])
            {
                try
                {
                    _allUserRoles[$"#{expectableUserRoleNamesProvider.GroupName}"] =
                        expectableUserRoleNamesProvider.ExpectableUserRoles().Result.ToArray();
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error on searching for datalinq roles");
                    ErrorMessage = String.IsNullOrEmpty(ErrorMessage)
                        ? $"DataLinq maybe corrupt: {ex.Message}"
                        : $"{ErrorMessage}{Environment.NewLine}DataLinq maybe corrupt: {ex.Message}";
                }
            }

            #endregion
        }
    }

    public void Clear(IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                      string cmsName = "")
    {
        lock (_lockThis)
        {
            ClearUnlocked(expectableUserRolesNamesProviders, cmsName);
        }
    }

    public void ClearCustomCaches(string cmsName = "")
    {
        _customCacheServices.ClearCache(cmsName);
    }

    private void ClearUnlocked(IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                               string cmsName = "")
    {
        if (String.IsNullOrWhiteSpace(cmsName))
        {
            _isInitialized = false;

            Init(expectableUserRolesNamesProviders);

            _generalCache.Clear();

            _customCacheServices.ClearCache(cmsName);
        }
        else if (_cacheInstance != null)
        {
            _cacheInstance.Reload(cmsName);
        }
    }

    public bool IsInitialized => _isInitialized;

    public CmsDocumentsService CmsDocuments => _cmsDocuments;

    internal IServiceProvider ServiceProvider => _serviceProvider;

    public WebGIS.Core.Models.ApiInfoDTO.CacheInfo CacheInfo
    {
        get
        {
            if (_cacheInstance != null)
            {
                return _cacheInstance.CacheInfo;
            }

            return new WebGIS.Core.Models.ApiInfoDTO.CacheInfo();
        }
    }

    public void Log(LogLevel level, string message, params object[] args)
    {
        if (_logger.IsEnabled(level))
        {
            _logger.Log(level, message, args);
        }
    }

    #region UserRoles

    public string[] AllCmsUserRoles()
    {
        UniqueList uList = new UniqueList();

        foreach (var key in _allUserRoles.Keys)
        {
            uList.AddRange(_allUserRoles[key]);
        }

        return uList.ToArray();
    }

    public ConcurrentDictionary<string, string[]> AllUserRoles => _allUserRoles;

    #endregion

    #region General Cache

    public T Get<T>(string key)
    {
        try
        {
            if (_generalCache.ContainsKey(key))
            {
                return (T)_generalCache[key];
            }
        }
        catch { }

        return default(T);
    }

    public void Set(string key, object obj)
    {
        _generalCache[key] = obj;
    }

    public string GdiSchemeDefault { get; internal set; }

    public IEnumerable<CmsItemDTO> CmsItems(CmsDocument.UserIdentification ui)
    {
        if (_cacheInstance != null)
        {
            return _cacheInstance.CmsItems(ui);
        }

        return new CmsItemDTO[0];
    }

    public bool IsCorrupt => _isCorrupt;

    public string ErrorMessage { get; set; }

    public string LastInitErrorMessage { get; set; }

    public IEnumerable<string> Warnings => _cacheInstance?.Warnings;

    #endregion

    #region Services

    async private Task InitServiceAsync(CacheInstance cacheInstance, IMapService service, string serviceKey, IUrlHelperService urlHelper, bool force = false)
    {
        StringBuilder errorMessage = new StringBuilder();

        try
        {
            using (var mutex = await FuzzyMutexAsync.LockAsync(serviceKey))
            {
                if (!force && ServiceIsIntialized(cacheInstance, service, serviceKey))
                {
                    return;
                }

                if (service is IMapService2)
                {
                    ((IMapService2)service).LayerProperties = GetServiceLayerProperties(serviceKey);
                }

                Map map = new Map("init");
                map.Environment.SetUserValue("WebAppPath", ApiGlobals.AppRootPath);
                map.Environment.SetUserValue(webgisConst.EtcPath, ApiGlobals.AppEtcPath);
                map.Environment.SetUserValue(webgisConst.OutputPath, urlHelper?.OutputPath());
                map.Environment.SetUserValue(webgisConst.OutputUrl, urlHelper?.OutputUrl());
                //map.SetUserValue(webgisConst.UserIdentification, ui);

                using (var serviceProviderScope = this.ServiceProvider.CreateScope())
                {
                    var requestContext = serviceProviderScope.ServiceProvider
                                                          .GetRequiredService<IRequestContext>();

                    if (await service.InitAsync(map, requestContext) == false)
                    {
                        errorMessage.Append($"Can't initialize geo-service.");
                    }

                    var layerProperties = GetServiceLayerProperties(serviceKey);
                }

                if (service.HasDiagnosticWarnings())
                {
                    errorMessage.Append(service.DiagnosticMessage());
                }

                if (!String.IsNullOrEmpty(serviceKey) && cacheInstance != null)
                {
                    cacheInstance.SetServiceIntializationTimeNow(serviceKey);
                }

                if (errorMessage.Length > 0)
                {
                    _logger.LogWarning(errorMessage.ToString());
                }


            }
        }
        catch (Exception ex)
        {
            errorMessage.Append($"Exception@InitServiceAsync: {ex.Message}");

            if (errorMessage.Length > 0)
            {
                _warningsLogger.LogString(
                    CmsDocument.UserIdentification.Anonymous,
                    String.Empty, serviceKey, "InitService", errorMessage.ToString());
            }

            throw;
        }
    }

    private bool ServiceIsIntialized(CacheInstance cacheInstance, IMapService service, string serviceKey)
    {
        if (cacheInstance == null)
        {
            return false;
        }

        if (service.HasInitialzationErrors())
        {
            // Wenn Dienst nicht geladen werden konnte => nicht immer neu probiern => 1 x pro Minute, bis sich Dienst eventuell erholt
            if (cacheInstance.ServiceIntializationTotalSeconds(serviceKey) < 60.0)
            {
                return true;
            }

            return false;
        }

        var hasLayers = service?.Layers != null && !service.Layers.IsEmpty;

        if (hasLayers)
        {
            return true;
        }

        if (cacheInstance.HasServiceIntializationTime(serviceKey))
        {
            return true;
        }

        return false;
    }

    internal DateTime? ServiceIntializationTimeUtc(string serviceKey)
    {
        if (_cacheInstance == null)
        {
            return null;
        }

        return _cacheInstance.ServiceIntializationTimeUtc(serviceKey);
    }

    async public Task<IMapService> GetService(string url, IMap parent, CmsDocument.UserIdentification ui, IUrlHelperService urlHelper)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._mapServices.ContainsKey(url))
        {
            return null;
        }

        IMapService service = cmsCacheItem._mapServices[url].QueryObject(ui);
        if (service == null)
        {
            return null;
        }

        string serviceKey = String.IsNullOrEmpty(ui?.Branch)
            ? url
            : $"{url}${ui.Branch}";

        if (!ServiceIsIntialized(cmsCacheItem.CacheInstance, service, serviceKey))
        {
            await InitServiceAsync(cmsCacheItem.CacheInstance, service, serviceKey, urlHelper);
        }

        return service.Clone(parent);
    }

    async public Task<(IMapService service, IEnumerable<LayerPropertiesDTO> serviceLayerProperties)> GetServiceAndLayerProperties(string url, IMap parent, CmsDocument.UserIdentification ui, IUrlHelperService urlHelper)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._mapServices.ContainsKey(url))
        {
            return (null, null);
        }

        IMapService service = cmsCacheItem._mapServices[url].QueryObject(ui);
        if (service == null)
        {
            return (null, null);
        }

        if (!ServiceIsIntialized(cmsCacheItem.CacheInstance, service, url))
        {
            await InitServiceAsync(cmsCacheItem.CacheInstance, service, url, urlHelper);
        }

        service = service.Clone(parent);

        var serviceLayerProperties = (cmsCacheItem._layerProperties.ContainsKey(url)) ?
            cmsCacheItem._layerProperties[url].ToArray() : // ThreadSafe Copy
            new LayerPropertiesDTO[0];

        return (service, serviceLayerProperties);
    }

    public bool IsServiceLayerHidden(string serviceId, string layerId, CmsDocument.UserIdentification ui)
    {
        var auth = GetBoolPropertyAuthorization($"{serviceId}::{layerId}::hidden", ui);
        if (auth == null)
        {
            return false;
        }

        return !auth.AuthorizedPropertyValue(ui);
    }

    //public bool CheckLayerAuthVisibility(string url, string layerId, CmsDocument.UserIdentification ui)
    //{
    //    var cmsCacheItem = GetCmsCacheItem(url, ui);

    //    if (cmsCacheItem == null)
    //    {
    //        return false;
    //    }

    //    if (cmsCacheItem._layerAuthVisibility.ContainsKey(url))
    //    {
    //        var property = layerId.LayerVisibilityAutPropertyId();

    //        foreach (var authProperty in cmsCacheItem._layerAuthVisibility[url].Where(p => p.Property == property))
    //        {
    //            if (authProperty.AuthorizedPropertyValue(ui) == false)
    //            {
    //                return false;
    //            }
    //        }
    //    }

    //    return true;
    //}

    public IEnumerable<string> GetUnauthorizedLayerIds(IMapService service, CmsDocument.UserIdentification ui, IEnumerable<string> layerIds = null)
    {
        if (layerIds == null)  // alle Layer des Dienstes testen
        {
            layerIds = service?.Layers?.Select(l => l.ID);
        }

        var cmsCacheItem = GetCmsCacheItem(service?.Url, ui);

        if (cmsCacheItem == null || layerIds == null || service == null)
        {
            return new string[0];
        }

        List<string> unauthorizedLayerIds = new List<string>();

        #region add layer/themes, where cms-theme-node is not authorized

        if (cmsCacheItem._layerAuthVisibility.ContainsKey(service.Url))
        {
            foreach (var layerId in layerIds)
            {
                var property = layerId.LayerVisibilityAuthPropertyId();

                foreach (var authProperty in cmsCacheItem._layerAuthVisibility[service.Url].Where(p => p.Property == property))
                {
                    if (authProperty.AuthorizedPropertyValue(ui) == false)
                    {
                        unauthorizedLayerIds.Add(layerId);
                        continue;
                    }
                }
            }
        }

        #endregion

        return unauthorizedLayerIds;
    }

    public bool HasServiceAccess(string url, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._mapServices.ContainsKey(url))
        {
            return false;
        }

        return cmsCacheItem._mapServices[url].QueryObject(ui) is not null;
    }

    async public Task<IMapService> GetOriginalService(string url, CmsDocument.UserIdentification ui, IUrlHelperService urlHelper)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._mapServices.ContainsKey(url))
        {
            return null;
        }

        IMapService service = cmsCacheItem._mapServices[url].QueryObject(ui);
        if (service is not null
            && !ServiceIsIntialized(cmsCacheItem.CacheInstance, service, url))
        {
            await InitServiceAsync(cmsCacheItem.CacheInstance, service, url, urlHelper);
        }

        return service;
    }

    public IMapService GetOriginalServiceFast(string url, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._mapServices.ContainsKey(url))
        {
            return null;
        }

        IMapService service = cmsCacheItem._mapServices[url].QueryObject(ui);

        return service;
    }

    public IMapService GetOriginalServiceIfInitialized(string url, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._mapServices.ContainsKey(url))
        {
            return null;
        }

        IMapService service = cmsCacheItem._mapServices[url].QueryObject(ui);
        if (ServiceIsIntialized(cmsCacheItem.CacheInstance, service, url))
        {
            return service;
        }

        return null;
    }

    public IMapService GetOriginalIgnoreInitialisation(string url, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._mapServices.ContainsKey(url))
        {
            return null;
        }

        return cmsCacheItem._mapServices[url].QueryObject(ui);
    }

    async public Task<IMapService> GetOriginalServiceDontReIntializeIfErrors(string url, CmsDocument.UserIdentification ui, IUrlHelperService urlHelper)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._mapServices.ContainsKey(url))
        {
            return null;
        }

        IMapService service = cmsCacheItem._mapServices[url].QueryObject(ui);
        if (service != null && !service.HasInitialzationErrors() && !ServiceIsIntialized(cmsCacheItem.CacheInstance, service, url))
        {
            await InitServiceAsync(cmsCacheItem.CacheInstance, service, url, urlHelper);
        }

        return service;
    }

    public IMapService[] GetServices(CmsDocument.UserIdentification ui)
    {
        CheckForContentAndInitStatic(String.Empty);

        List<IMapService> services = new List<IMapService>();

        foreach (var cmsCacheItem in CacheInstanceItems.AllVisibleItems(this, ui))
        {
            if (cmsCacheItem == null)
            {
                continue;
            }

            if ((ui.RequestsBranch() && !cmsCacheItem.BelongsToBranch(ui.Branch)) ||
                (!ui.RequestsBranch() && !cmsCacheItem.BelongsToBranch(null)))
            {
                // only show requested branch or "main"
                continue;
            }

            foreach (var authService in cmsCacheItem._mapServices.Values)
            {
                var service = authService.QueryObject(ui);
                if (service != null)
                {
                    services.Add(service);
                }
            }
        }

        return services.ToArray();
    }

    public string ServiceCmsTocName(string id)
    {
        var cmsCacheItem = GetCmsCacheItem(id, null);
        if (cmsCacheItem == null || !cmsCacheItem._tocName.ContainsKey(id))
        {
            return String.Empty;
        }

        return cmsCacheItem._tocName[id];
    }

    public bool ServiceHasCustomCmsToc(string id)
    {
        var tocName = ServiceCmsTocName(id);

        return String.IsNullOrWhiteSpace(tocName) == false && tocName != "default";
    }

    public bool IsWmsExportable(ServiceInfoDTO info, CmsDocument.UserIdentification ui)
    {
        return IsWmsExportable(info.id, ui);
    }
    public bool IsWmsExportable(string serviceId, CmsDocument.UserIdentification ui)
    {
        var auth = GetBoolPropertyAuthorization(serviceId + "::exportwms", ui);
        if (auth == null)
        {
            return false;
        }

        return auth.AuthorizedPropertyValue(ui);
    }

    public string CmsItemDisplayName(string id, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(id, ui);
        return cmsCacheItem?.DisplayName;
    }

    public LayerPropertiesDTO GetLayerProperties(string id, string layerId)
    {
        var cmsCacheItem = GetCmsCacheItem(id, null);

        if (cmsCacheItem == null || !cmsCacheItem._layerProperties.ContainsKey(id))
        {
            return null;
        }

        return cmsCacheItem._layerProperties[id]
                    .ToArray() // ThreadSafe
                    .Where(l => l.Id == layerId)
                    .FirstOrDefault();
    }

    public IEnumerable<LayerPropertiesDTO> GetServiceLayerProperties(string id)
    {
        var cmsCacheItem = GetCmsCacheItem(id, null);

        if (cmsCacheItem == null || !cmsCacheItem._layerProperties.ContainsKey(id))
        {
            return new LayerPropertiesDTO[0];
        }

        return cmsCacheItem._layerProperties[id].ToArray(); // ThreadSafe Copy
    }

    #endregion

    #region Copyright

    public CopyrightInfoDTO CopyrightInfo(string id, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(id, ui);

        return cmsCacheItem?._copyright.Where(c => c.Id == id).FirstOrDefault();
    }

    //public static CopyrightInfo CopyrightInfo(ServiceInfo serviceInfo)
    //{
    //    return CopyrightInfo(serviceInfo?.copyright);
    //}

    public IEnumerable<CopyrightInfoDTO> CopyrightInfo(IEnumerable<ServiceInfoDTO> serviceInfos, CmsDocument.UserIdentification ui)
    {
        List<CopyrightInfoDTO> copyrightInfos = new List<CopyrightInfoDTO>();

        foreach (var serviceInfo in serviceInfos)
        {
            if (serviceInfo == null || String.IsNullOrWhiteSpace(serviceInfo.copyright))
            {
                continue;
            }

            foreach (var copyrightId in serviceInfo.copyright.Split(',').Select(c => c.Trim()))
            {
                if (copyrightInfos.Where(c => c.Id == copyrightId).Count() > 0)
                {
                    continue;
                }

                var copyrightInfo = CopyrightInfo(copyrightId, ui);
                if (copyrightInfo != null)
                {
                    copyrightInfos.Add(copyrightInfo);
                }
            }

            //if (copyrightInfos.Where(c => c.Id == serviceInfo?.copyright).Count() > 0)
            //    continue;

            //var copyrightInfo = CopyrightInfo(serviceInfo);
            //if (copyrightInfo != null)
            //    copyrightInfos.Add(copyrightInfo);
        }

        return copyrightInfos;
    }

    #endregion

    #region Add Services Containers

    public bool HasAddServiceContainers(CmsDocument.UserIdentification ui)
    {
        return CacheInstanceItems.AllVisibleItems(this, ui).Select(c => c._serviceContainers.Count).Sum() > 0;
    }

    public string ServiceContainerName(string id, string gdiCustomScheme, CmsDocument.UserIdentification ui, bool useCmsNameIfUnknown = true)
    {
        var url = GdiCustomSchemeKey(id, gdiCustomScheme);

        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem != null && cmsCacheItem._serviceContainers.ContainsKey(url))
        {
            return cmsCacheItem._serviceContainers[url];
        }

        if (useCmsNameIfUnknown == false)
        {
            return String.Empty;
        }

        if (!String.IsNullOrWhiteSpace(cmsCacheItem?.DisplayName))
        {
            return cmsCacheItem.DisplayName;
        }

        if (id.Contains("@"))
        {
            return id.Split('@')[1];
        }

        return "CMS";
    }

    public void SortContainerServices(List<ServiceDTO> services, CmsDocument.UserIdentification ui)
    {
        var serviceContainers = new Dictionary<string, string>();

        foreach (var cmsCacheItem in CacheInstanceItems.AllVisibleItems(this, ui))
        {
            if (cmsCacheItem?._serviceContainers != null)
            {
                foreach (var key in cmsCacheItem._serviceContainers.Keys)
                {
                    if (!serviceContainers.ContainsKey(key))
                    {
                        serviceContainers.Add(key, cmsCacheItem._serviceContainers[key]);
                    }
                }
            }
        }

        services.Sort(new ServiceContainerSorter(serviceContainers));
    }

    private class ServiceContainerSorter : IComparer<ServiceDTO>
    {
        private readonly List<string> _keys;
        public ServiceContainerSorter(Dictionary<string, string> containers)
        {
            _keys = containers.Keys.ToList();
        }

        public int Compare(ServiceDTO x, ServiceDTO y)
        {
            var indexX = _keys.IndexOf(x.id);
            var indexY = _keys.IndexOf(y.id);

            if (indexX < 0 && indexY < 0)
            {
                return x.name.CompareTo(y.name);
            }

            if (indexX < 0)
            {
                return 1;
            }

            if (indexY < 0)
            {
                return -1;
            }

            return indexX.CompareTo(indexY);
        }
    }

    #endregion

    #region Extents

    public DTOs.ExtentDTO[] GetExtents(CmsDocument.UserIdentification ui)
    {
        CheckForContentAndInitStatic(String.Empty);

        return CacheInstanceItems
            .AllVisibleItems(this, ui)
            .SelectMany(c => c._extents.Values.ToArray())
            .ToArray();
    }

    public DTOs.ExtentDTO GetExtent(string url, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(url, ui);

        if (cmsCacheItem == null || !cmsCacheItem._extents.ContainsKey(url))
        {
            return null;
        }

        return cmsCacheItem._extents[url];
    }

    #endregion

    #region Presentations

    //public Presentations GetPresentations(string serviceUrl, string gdiCustomScheme, CmsDocument.UserIdentification ui)
    //{
    //    var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

    //    var url = GdiCustomSchemeKey(serviceUrl, gdiCustomScheme);
    //    if (cmsCacheItem != null && cmsCacheItem._presentations.ContainsKey(url))
    //    {
    //        var presentations = AuthObject<Presentation>.QueryObjectArray(cmsCacheItem._presentations[url], ui);

    //        return new Presentations()
    //        {
    //            presentations = presentations
    //        };
    //    }

    //    return new Presentations();
    //}

    public PresentationsDTO GetPresentations(IMapService service, string gdiCustomScheme, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(service?.Url, ui);

        if (cmsCacheItem != null && service != null)
        {
            var url = GdiCustomSchemeKey(service?.Url, gdiCustomScheme);
            if (cmsCacheItem._presentations.ContainsKey(url))
            {
                var presentations = AuthObject<PresentationDTO>.QueryObjectArray(cmsCacheItem._presentations[url], ui)
                                                            .Select(p => p.Clone())  // Clonen!!! Layers Eigenschaft kann je nach Berechtigung verändert werden
                                                            .ToArray();

                var unauthorizedLayerIds = this.GetUnauthorizedLayerIds(service, ui);

                foreach (var presentation in presentations.Where(p => p.layers != null))
                {
                    if (service.IsBaseMap == false)
                    {
                        //
                        // Layer, für die man nicht berechtigt ist aus der Darstellungsvariante entfernen
                        //
                        List<string> layerNames = new List<string>();
                        foreach (var layerName in presentation.layers)
                        {
                            var layer = service.Layers.FindByName(layerName);
                            if (layer != null && !unauthorizedLayerIds.Contains(layer.ID))
                            {
                                layerNames.Add(layerName);
                            }
                        }

                        presentation.layers = layerNames.ToArray();
                    }
                }

                return new PresentationsDTO()
                {
                    presentations = presentations.Where(p => p.IsEmpty == true || (p.layers != null && p.layers.Count() > 0))
                                                 .ToArray()
                };
            }
        }

        return new PresentationsDTO();
    }

    #endregion

    #region Queries

    public DTOs.QueriesDTO GetQueries(string serviceUrl, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null && cmsCacheItem._queries.ContainsKey(serviceUrl))
        {
            return new DTOs.QueriesDTO()
            {
                queries = AuthObject<QueryDTO>.QueryObjectArray(cmsCacheItem._queries[serviceUrl], ui) //  retQueries.ToArray()
            };
        }

        return new DTOs.QueriesDTO();
    }

    async public Task<QueryDTO> GetQuery(string serviceUrl, string queryId, CmsDocument.UserIdentification ui, IUrlHelperService urlHelper)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null)
        {
            if (cmsCacheItem._queries.ContainsKey(serviceUrl))
            {
                foreach (var query in AuthObject<QueryDTO>.QueryObjectArray(cmsCacheItem._queries[serviceUrl], ui))
                {
                    if (query != null && query.id == queryId)
                    {
                        if (query.Service == null)
                        {
                            IMapService service = await GetOriginalService(serviceUrl, ui, urlHelper: urlHelper);
                            if (service != null)
                            {
                                query.Init(service);
                            }
                        }

                        var clone = query.AuthClone(ui);  // ToDo: AuthClone is alread be done in AuthObject<Query>.QueryObjectArray, why here again?!
                        clone.Service = query.Service;    // because it shoud not be changable inside customers/callers => makes sense...

                        return clone;
                    }
                }
            }
            else
            {
                var service = await this.GetOriginalService(serviceUrl, ui, urlHelper);

                if (service is IDynamicService && ((IDynamicService)service).CreateQueriesDynamic != ServiceDynamicQueries.Manually)
                {
                    return ((IDynamicService)service).GetDynamicQuery(queryId);
                }
            }
        }

        return null;
    }

    async public Task<QueryDTO> GetQueryTemplate(string serviceUrl, string queryId, CmsDocument.UserIdentification ui, IUrlHelperService urlHelper)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null)
        {
            if (cmsCacheItem._queries.ContainsKey(serviceUrl))
            {
                foreach (var query in AuthObject<QueryDTO>.QueryObjectArray(cmsCacheItem._queries[serviceUrl], ui))
                {
                    if (query != null && query.id == queryId)
                    {
                        var clone = query.AuthClone(ui);

                        return clone;
                    }
                }
            }
            else
            {
                var service = await this.GetOriginalServiceDontReIntializeIfErrors(serviceUrl, ui, urlHelper);
                if (service is IDynamicService && ((IDynamicService)service).CreateQueriesDynamic != ServiceDynamicQueries.Manually)
                {
                    return ((IDynamicService)service).GetDynamicQueryTemplate(queryId);
                }
            }
        }

        return null;
    }

    #endregion

    #region EditThemes

    public EditThemesDTO GetEditThemes(string serviceUrl, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null && cmsCacheItem._editThemes.ContainsKey(serviceUrl))
        {
            return new EditThemesDTO()
            {
                editthemes = AuthObject<EditThemeDTO>.QueryObjectArray(cmsCacheItem._editThemes[serviceUrl], ui)
            };
        }

        return new EditThemesDTO();
    }

    public EditThemeDTO GetEditTheme(string serviceUrl, string themeId, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null && cmsCacheItem._editThemes.ContainsKey(serviceUrl))
        {
            var themes = AuthObject<EditThemeDTO>.QueryObjectArray(cmsCacheItem._editThemes[serviceUrl], ui);
            if (themes != null)
            {
                return (from t in themes where t.ThemeId == themeId select t).FirstOrDefault();
            }
        }

        return null;
    }

    #endregion

    #region Vis Filters

    public VisFiltersDTO GetVisFilters(string serviceUrl, CmsDocument.UserIdentification ui, VisFilterType type = VisFilterType.visible)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null && cmsCacheItem._visFilters.ContainsKey(serviceUrl))
        {

            return new VisFiltersDTO()
            {
                filters = (from f in AuthObject<VisFilterDTO>.QueryObjectArray(cmsCacheItem._visFilters[serviceUrl], ui) where f.FilterType == type select f).ToArray()
            };
        }

        return new VisFiltersDTO()
        {
            filters = new VisFilterDTO[0]
        };
    }

    public VisFiltersDTO GetAllVisFilters(string serviceUrl, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null && cmsCacheItem._visFilters.ContainsKey(serviceUrl))
        {

            return new VisFiltersDTO()
            {
                filters = AuthObject<VisFilterDTO>.QueryObjectArray(cmsCacheItem._visFilters[serviceUrl], ui)
            };
        }

        return new VisFiltersDTO()
        {
            filters = new VisFilterDTO[0]
        };
    }

    #endregion

    #region Labeling

    public LabelingsDTO GetLabeling(string serviceUrl, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null && cmsCacheItem._labeling.ContainsKey(serviceUrl))
        {
            return new LabelingsDTO()
            {
                labelings = AuthObject<LabelingDTO>.QueryObjectArray(cmsCacheItem._labeling[serviceUrl], ui).ToArray()
            };
        }

        return new LabelingsDTO();
    }

    #endregion

    #region SnapSchemes 

    public IEnumerable<SnapSchemaDTO> GetSnapSchemes(string serviceUrl, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null && cmsCacheItem._snapSchemes.ContainsKey(serviceUrl))
        {
            return AuthObject<SnapSchemaDTO>.QueryObjectArray(cmsCacheItem._snapSchemes[serviceUrl], ui).ToArray();
        }

        return new SnapSchemaDTO[0];
    }

    #endregion

    #region Chainage Themes

    public IEnumerable<ChainageThemeDTO> GetServiceChainageThemes(string serviceUrl, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(serviceUrl, ui);

        if (cmsCacheItem != null && cmsCacheItem._chainageThemes.ContainsKey(serviceUrl))
        {
            return AuthObject<ChainageThemeDTO>.QueryObjectArray(cmsCacheItem._chainageThemes[serviceUrl], ui);
        }

        return new ChainageThemeDTO[0];
    }

    public ChainageThemeDTO GetChainageTheme(string id, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(id, ui);

        if (cmsCacheItem != null)
        {
            foreach (string serviceUrl in cmsCacheItem._chainageThemes.Keys)
            {
                var theme = AuthObject<ChainageThemeDTO>.QueryObjectArray(cmsCacheItem._chainageThemes[serviceUrl], ui)?.Where(c => c.Id == id).FirstOrDefault();
                if (theme != null)
                {
                    return theme;
                }
            }
        }
        return null;
    }

    #endregion

    #region Print Layouts/Formats

    public IEnumerable<PrintLayoutDTO> GetPrintLayouts(string gdiCustomScheme, CmsDocument.UserIdentification ui)
    {
        CheckForContentAndInitStatic(String.Empty);

        List<PrintLayoutDTO> layouts = new List<PrintLayoutDTO>();

        foreach (var cmsCacheItem in CacheInstanceItems.AllVisibleItems(this, ui))
        {
            if (cmsCacheItem == null)
            {
                continue;
            }

            foreach (var authLayout in cmsCacheItem._printLayouts.Values)
            {
                var layout = authLayout.QueryObject(ui);

                if (layout == null)
                {
                    continue;
                }

                var url = GdiCustomSchemeKey(layout.Id, gdiCustomScheme);
                if (!cmsCacheItem._printLayouts.ContainsKey(url))
                {
                    continue;
                }

                if (layout != null && layouts.Where(l => l.Id == layout.Id).Count() == 0)  // doppelete Layout vermeiden
                {
                    layouts.Add(layout);
                }
            }
        }

        return layouts;
    }

    public IEnumerable<PrintFormatDTO> GetPrintFormats(string gdiCustomScheme, CmsDocument.UserIdentification ui)
    {
        CheckForContentAndInitStatic(String.Empty);

        List<PrintFormatDTO> formats = new List<PrintFormatDTO>();

        var url = GdiCustomSchemeKey(String.Empty, gdiCustomScheme);

        foreach (var cmsCacheItem in CacheInstanceItems.AllVisibleItems(this, ui))
        {
            if (cmsCacheItem == null)
            {
                continue;
            }

            if (cmsCacheItem._printFormats.ContainsKey(url))
            {
                foreach (var authFormat in cmsCacheItem._printFormats[url])
                {
                    var format = authFormat.QueryObject(ui);
                    if (format == null || formats.Where(f => f.Size == format.Size && f.Orientation == format.Orientation).Count() > 0)
                    {
                        continue;
                    }

                    formats.Add(format);
                }
            }
        }

        return formats;
    }

    #endregion

    #region Tools

    public IApiButton[] GetApiTools(string client)
    {
        CheckForContentAndInitStatic(String.Empty);

        client = (client ?? String.Empty).ToLower();
        bool isInDeveloperMode = ApiGlobals.IsInDeveloperMode;

        var tools = _cacheInstance?.Tools;
        if (tools == null)
        {
            return new IApiButton[0];
        }

        return tools.Values
            .ToArray()  // ThreadSafe
            .Where(t => (t.GetType().GetCustomAttribute<ToolClientAttribute>()?.ClientName?.ToLower() ?? String.Empty) == client)
            .Where(t => t.GetType().GetCustomAttribute<InDevelopmentAttribute>() == null || isInDeveloperMode)
            .ToArray();
    }

    public IApiButton GetTool(string typeName)
    {
        CheckForContentAndInitStatic(String.Empty);

        typeName = typeName.ToNewToolId();

        var tools = _cacheInstance?.Tools;
        if (tools != null)
        {
            if (tools.ContainsKey(typeName))
            {
                return tools[typeName];
            }
        }

        return null;
    }

    public object ToolConfigValue(string toolConfigKey, CmsDocument.UserIdentification ui)
    {
        foreach (var cmsCacheItem in CacheInstanceItems.Values.ToArray())   // ToArray: ThreadSafe
        {
            if (cmsCacheItem == null)
            {
                continue;
            }

            if (cmsCacheItem._toolConfig.ContainsKey(toolConfigKey))
            {
                return cmsCacheItem._toolConfig[toolConfigKey];
            }

            foreach (string key in cmsCacheItem._toolConfig.Keys)
            {
                if (key.StartsWith(toolConfigKey + "@"))
                {
                    return cmsCacheItem._toolConfig[key];
                }
            }
        }

        return null;
    }

    public object[] ToolConfigValues(string toolConfigKey, CmsDocument.UserIdentification ui)
    {
        List<object> values = new List<object>();

        foreach (var cmsCacheItem in CacheInstanceItems.AllVisibleItems(this, ui))
        {
            if (cmsCacheItem == null)
            {
                continue;
            }

            foreach (string key in cmsCacheItem._toolConfig.Keys)
            {
                if (key == toolConfigKey || key.StartsWith(toolConfigKey + "@"))
                {
                    values.Add(cmsCacheItem._toolConfig[key]);
                }
            }
        }

        return values.ToArray();
    }

    public CacheInstance.ToolResource GetToolResource(string key)
    {
        return _cacheInstance?.GetToolResource(key);
    }

    #endregion

    #region Search Services

    public ISearchService[] GetSearchServices(CmsDocument.UserIdentification ui, string[] serviceIds = null)
    {
        CheckForContentAndInitStatic(String.Empty);

        List<ISearchService> services = new List<ISearchService>();

        foreach (var cmsCacheItem in CacheInstanceItems.AllVisibleItems(this, ui))
        {
            if (cmsCacheItem == null)
            {
                continue;
            }

            foreach (var authService in cmsCacheItem._searchServices.Values)
            {
                var service = authService.QueryObject(ui);
                if (service != null)
                {
                    if (serviceIds != null && serviceIds.Where(id => id.ToLower() == service.Id.ToLower()).Count() == 0)
                    {
                        continue;
                    }

                    services.Add(service);
                }
            }
        }

        return services.ToArray();
    }

    public ISearchService GetSearchService(string id, CmsDocument.UserIdentification ui)
    {
        var cmsCacheItem = GetCmsCacheItem(id, ui);

        if (cmsCacheItem == null || !cmsCacheItem._searchServices.ContainsKey(id))
        {
            return null;
        }

        var service = cmsCacheItem._searchServices[id].QueryObject(ui);
        if (service == null)
        {
            return null;
        }

        return service;
    }

    #endregion

    #region Transformations

    public IEnumerable<DTOs.Transformations.Helmert2dDTO> Helmert2dTransformations
    {
        get
        {
            return _cacheInstance?.Helmert2dTransformations ?? new DTOs.Transformations.Helmert2dDTO[0];
        }
    }

    #endregion

    #region Helper

    private void CheckForContentAndInitStatic(string cmsName)
    {
        if (_cacheInstance != null)
        {
            _cacheInstance.CheckForContentAndInitStatic(cmsName);
        }
        else
        {
            using (var serviceProviderScope = _serviceProvider.CreateScope())
            {
                var expectableRoleNamesProviders = serviceProviderScope
                                                        .ServiceProvider
                                                        .GetRequiredService<IEnumerable<IExpectableUserRoleNamesProvider>>();

                Init(expectableRoleNamesProviders);
            }
        }
    }

    internal CmsCacheItem LoadCustomCmsCacheItem(string cmsName)
    {
        return _cacheInstance?.LoadCustomCmsCacheItem(cmsName);
    }

    private IDictionary<string, CmsCacheItem> CacheInstanceItems
    {
        get
        {
            if (_cacheInstance != null)
            {
                return _cacheInstance.CmsCacheItemsDictionary;
            }

            return new Dictionary<string, CmsCacheItem>();
        }
    }

    private AuthProperty<bool> GetBoolPropertyAuthorization(string property, CmsDocument.UserIdentification ui)
    {
        string id = property;
        if (id.Contains("::"))
        {
            id = property.Substring(0, property.IndexOf("::"));
        }

        var cmsCacheItem = GetCmsCacheItem(id, ui);
        if (cmsCacheItem == null)
        {
            return null;
        }

        return (from p in cmsCacheItem._authBoolProperties where p.Property == property select p).FirstOrDefault();
    }

    internal string GdiCustomSchemeKey(string key, string customScheme)
    {
        if (String.IsNullOrWhiteSpace(customScheme) || customScheme == "~" || customScheme == "_root")
        {
            return key;
        }

        if (String.IsNullOrWhiteSpace(key))
        {
            return customScheme;
        }

        return key + "." + customScheme;
    }

    private CmsCacheItem GetCmsCacheItem(string id, CmsDocument.UserIdentification ui)
    {
        if (_cacheInstance != null)
        {
            return _cacheInstance.GetCmsCacheItem(id, ui);
        }

        return null;
    }

    #endregion
}