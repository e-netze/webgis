using E.Standard.Api.App.DTOs;
using E.Standard.Api.App.Extensions;
using E.Standard.CMS.Core;
using E.Standard.Json;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using gView.GraphicsEngine;
using gView.GraphicsEngine.Abstraction;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Services.Cache;

public class CacheInstance
{
    //private Dictionary<string, CmsDocument> _allCms = new Dictionary<string, CmsDocument>();
    private int _loadedCmsCount = 0;

    private readonly ConcurrentDictionary<string, IApiButton> _tools = new ConcurrentDictionary<string, IApiButton>();
    private readonly ConcurrentDictionary<string, CacheInstance.ToolResource> _resourceContainer = new ConcurrentDictionary<string, CacheInstance.ToolResource>();
    private ConcurrentBag<E.Standard.Api.App.DTOs.Transformations.Helmert2dDTO> _helmert2dTransformations = new ConcurrentBag<E.Standard.Api.App.DTOs.Transformations.Helmert2dDTO>();
    private readonly ConcurrentDictionary<string, CmsCacheItem> _cmsCacheItems = new ConcurrentDictionary<string, CmsCacheItem>();

    private static DateTime _intitialTime = new DateTime();

    private bool _isCorrupt = false;
    private string _errorMessage;
    private bool _isInitialized = false;

    private readonly CacheService _cacheService;
    private readonly ConcurrentDictionary<string, DateTime> _serviceInitializationTime = new ConcurrentDictionary<string, DateTime>();

    public CacheInstance(CacheService cache)
    {
        _cacheService = cache;
    }

    #region Properties

    public bool IsCorrupt => _isCorrupt || HasCorruptItems;

    public bool HasCorruptItems
    {
        get
        {
            foreach (var key in _cmsCacheItems.Keys)
            {
                var cacheItem = _cmsCacheItems[key];
                if (cacheItem.IsCorrupt)
                {
                    return true;
                }
            }

            return false;
        }
    }


    public string ErrorMessage
    {
        get
        {
            StringBuilder errorMessage = new StringBuilder();

            errorMessage.Append(_errorMessage);
            foreach (var key in _cmsCacheItems.Keys)
            {
                var cacheItem = _cmsCacheItems[key];
                if (cacheItem.IsCorrupt)
                {
                    errorMessage.Append(System.Environment.NewLine);
                    errorMessage.Append($"CMS {key}: {_cmsCacheItems[key].ErrorMessage}");
                }
            }

            return errorMessage.ToString();
        }
    }

    public IEnumerable<string> Warnings
    {
        get
        {
            List<string> warnings = new List<string>();

            foreach (var key in _cmsCacheItems.Keys)
            {
                var cacheItem = _cmsCacheItems[key];
                if (cacheItem._warnings != null && cacheItem._warnings.Count > 0)
                {
                    warnings.AddRange(cacheItem._warnings);
                }
            }

            return warnings.Count > 0 ? warnings : null;
        }
    }

    public WebGIS.Core.Models.ApiInfoDTO.CacheInfo CacheInfo
    {
        get
        {
            return new WebGIS.Core.Models.ApiInfoDTO.CacheInfo()
            {
                InitialTime = _intitialTime,
                CmsCount = _loadedCmsCount,
                ServicesCount = _cmsCacheItems.Values.ToArray().Select(c => c._mapServices.Count).Sum(),
                ExtentsCount = _cmsCacheItems.Values.ToArray().Select(c => c._extents.Count).Sum(),
                PresentationsCount = _cmsCacheItems.Values.ToArray().Select(c => c._presentations.Count).Sum(),
                QueriesCount = _cmsCacheItems.Values.ToArray().Select(c => c._queries.Count).Sum(),
                ToolsCount = _tools.Count,
                SearchServicCount = _cmsCacheItems.Values.ToArray().Select(c => c._searchServices.Count).Sum(),
                EditThemesCount = _cmsCacheItems.Values.ToArray().Select(c => c._editThemes.Count).Sum(),
                VisFiltersCount = _cmsCacheItems.Values.ToArray().Select(c => c._editThemes.Count).Sum()
            };
        }
    }

    public IEnumerable<CmsItemDTO> CmsItems(CmsDocument.UserIdentification ui)
    {
        CheckForContentAndInitStatic(String.Empty);

        return _cmsCacheItems.Values
            .ToArray()  // ThreadSafe
            .Where(i =>
            {
                if (i.IsCustom == true)
                {
                    return ui != null && ui.UserRolesCmsNames().Contains(i.Name);
                }

                return true;
            })
            .Select(i =>
            {
                return new CmsItemDTO()
                {
                    Name = String.IsNullOrWhiteSpace(i.Name) ? "CMS" : i.Name,
                    ErrorMessage = String.IsNullOrWhiteSpace(i.ErrorMessage) ? null : i.ErrorMessage,
                    ServicesCount = i._mapServices.Count(),
                    Status = i.IsCorrupt ? CmsItemStatus.Corrupt : CmsItemStatus.Ok,
                    InitializationTime = i._intitialTime
                };
            });
    }

    internal IDictionary<string, CmsCacheItem> CmsCacheItemsDictionary
    {
        get { return _cmsCacheItems; }
    }

    internal IDictionary<string, IApiButton> Tools
    {
        get
        {
            return _tools;
        }
    }

    internal IEnumerable<DTOs.Transformations.Helmert2dDTO> Helmert2dTransformations
    {
        get
        {
            CheckForContentAndInitStatic(String.Empty);

            return _helmert2dTransformations.ToArray(); // ToArray for make it Threadsafe
        }
    }

    internal ToolResource GetToolResource(string key)
    {
        if (_resourceContainer.ContainsKey(key))
        {
            return _resourceContainer[key];
        }

        return null;
    }

    #region Service Initialization Time

    internal void SetServiceIntializationTimeNow(string serviceKey)
    {
        _serviceInitializationTime[serviceKey] = DateTime.UtcNow;
    }

    internal bool HasServiceIntializationTime(string serviceKey)
    {
        if (String.IsNullOrEmpty(serviceKey))
        {
            return false;
        }

        return _serviceInitializationTime.ContainsKey(serviceKey);
    }

    internal double ServiceIntializationTotalSeconds(string serviceKey)
    {
        try
        {
            if (!HasServiceIntializationTime(serviceKey))
            {
                return double.MaxValue;
            }

            return (DateTime.UtcNow - _serviceInitializationTime[serviceKey]).TotalSeconds;
        }
        catch
        {
            return double.MaxValue;
        }
    }

    internal DateTime? ServiceIntializationTimeUtc(string serviceKey)
    {
        return HasServiceIntializationTime(serviceKey) ? _serviceInitializationTime[serviceKey] : null;
    }

    #endregion

    #endregion

    #region Methods

    public void Init()
    {
        _isCorrupt = false;
        _errorMessage = String.Empty;

        try
        {
            if (_loadedCmsCount == 0)
            {
                _intitialTime = DateTime.UtcNow;

                #region Buttons

                WebGIS.Tools.PluginManager pman = new WebGIS.Tools.PluginManager(ApiGlobals.AppPluginsPath);

                //try
                {

                    foreach (var buttonType in pman.ApiButtonTypes)
                    {
                        if (_tools.ContainsKey(buttonType.ToString().ToLower()))
                        {
                            throw new Exception("Key for tools already exists: " + buttonType.ToString().ToLower());
                        }

                        IApiButton button = pman.CreateApiButtonInstance(buttonType);
                        if (!_tools.TryAdd(button.GetType().ToToolId(), button))
                        {
                            throw new Exception($"Can't add tool {button.GetType()}");
                        }

                        if (button is IApiButtonResources)
                        {
                            ToolResouceManager trm = new ToolResouceManager(buttonType, _resourceContainer);
                            ((IApiButtonResources)button).RegisterToolResources(trm);
                        }
                    }
                }
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.Message);
                //}

                #endregion

                var allCmsDocuments = _cacheService.CmsDocuments.AllCmsDocuments();
                _loadedCmsCount = allCmsDocuments.Count;

                //MapSession mapSession = new MapSession(ApiGlobals.MapApplication);
                //Map map = new Map(mapSession, "init", String.Empty, String.Empty, ApiGlobals.AppConfigPath);
                //mapSession.Map = map;
                //map.Environment.SetUserValue(webgisConst.AppConfigPath, ApiGlobals.AppConfigPath);
                //map.Environment.SetUserValue(webgisConst.EtcPath, ApiGlobals.AppEtcPath);
                //mapSession[webgisConst.WebAppPath] = ApiGlobals.MapApplication.ApplicationPath;

                foreach (string cmsName in allCmsDocuments.Keys)
                {
                    CmsDocument cms = allCmsDocuments[cmsName];
                    //CmsHlp cmsHlp = new CmsHlp(cms, map);

                    var cmsCacheItem = new CmsCacheItem(this);

                    _cacheService.Log(LogLevel.Information, "Init Cms {cmsName}", cmsName);

                    cmsCacheItem.Init(_cacheService, cmsName, cms, false);
                    _cacheService._allUserRoles[cmsName] = cms.AllRoles?.Distinct().ToArray() ?? new string[0];

                    _cacheService.Log(LogLevel.Information, "...succeeded (init cms)");

                    _cmsCacheItems.TryAdd(cmsName, cmsCacheItem);
                }

                #region Transformations

                try
                {
                    var fi = new FileInfo(Path.Combine(ApiGlobals.AppEtcPath, "trafo", "helmert2d.json"));
                    if (fi.Exists)
                    {
                        _cacheService.Log(LogLevel.Information, "Init Transformations");
                        var helmert2ds = JSerializer.Deserialize<DTOs.Transformations.Helmert2dDTO[]>(File.ReadAllText(fi.FullName));

                        var srsIds = helmert2ds.Select(h => h.SrsId).Distinct();
                        var sRef4326 = ApiGlobals.SRefStore.SpatialReferences.ById(4326);

                        foreach (int srsId in srsIds)
                        {
                            var sRef = ApiGlobals.SRefStore.SpatialReferences.ById(srsId);
                            using (var transformation = new WebMapping.Core.Geometry.GeometricTransformerPro(sRef, sRef4326))
                            {
                                foreach (var helmert2d in helmert2ds.Where(h => h.SrsId == srsId))
                                {
                                    var R = new WebMapping.Core.Geometry.Point(helmert2d.Rx, helmert2d.Ry);
                                    transformation.Transform(R);
                                    helmert2d.RLng = R.X;
                                    helmert2d.RLat = R.Y;
                                }
                            }
                        }

                        foreach (var helmert2d in helmert2ds)
                        {
                            _helmert2dTransformations.Add(helmert2d);
                        }
                        _cacheService.Log(LogLevel.Information, "...succeeded (init transformations)");
                    }
                }
                catch (Exception ex)
                {
                    _cacheService.Log(LogLevel.Error, "Init cache instance: {message}", ex.Message);
                }

                #endregion
            }

            if (_tools.Count == 0)
            {
                throw new Exception($"No tools found: rootPath = {ApiGlobals.AppRootPath}");
            }

            _isInitialized = true;

            _cacheService.Log(LogLevel.Information, "Cache instance initialized: {message}", JSerializer.Serialize(this.CacheInfo, pretty: true));
        }
        catch (Exception ex)
        {
            _isCorrupt = true;
            _errorMessage = ex.Message + System.Environment.NewLine + ex.StackTrace;

            _cacheService.Log(LogLevel.Error, "Currupt cache instance: {message}", _errorMessage);

            throw;
        }
    }

    public void Reload(string cmsName)
    {
        if (_cmsCacheItems.ContainsKey(cmsName))
        {
            var disposeItem = _cmsCacheItems[cmsName];

            CmsDocument cms = disposeItem.IsCustom ?
                                                    _cacheService.CmsDocuments.GetCustomCmsDocument(cmsName) :
                                                    _cacheService.CmsDocuments.GetCmsDocument(cmsName);
            if (cms == null)
            {
                throw new Exception("Can't open/find CMS.xml");
            }

            var cmsCacheItem = new CmsCacheItem(this);

            _cacheService.Log(LogLevel.Information, "Init Cms {cmsName}", cmsName);

            cmsCacheItem.Init(_cacheService, cmsName, cms, disposeItem.IsCustom);
            cmsCacheItem.DisplayName = disposeItem.DisplayName;

            _cacheService._allUserRoles[cmsName] = cms.AllRoles?.Distinct().ToArray() ?? new string[0];

            if (cmsCacheItem.IsCorrupt == true)
            {
                throw new Exception($"Can't refresh/init cache item {cmsName}: {cmsCacheItem.ErrorMessage}");
            }

            _cacheService.Log(LogLevel.Information, "...succeeded (init cms)");

            _cmsCacheItems[cmsName] = cmsCacheItem;
            var timeKeys = _serviceInitializationTime.Keys.Where(k => k.EndsWith($"@{cmsName}"));
            foreach (var key in timeKeys)
            {
                _cacheService.Log(LogLevel.Information, "try remove initializtionTime for {key}", key);

                if (_serviceInitializationTime.TryRemove(key, out DateTime dateTime))
                {
                    _cacheService.Log(LogLevel.Information, "...succeeded (remove initializtionTime for {key}): new {dateTime}", key, dateTime);
                }
            }


            Task.Run(async () =>
            {
                await Task.Delay(3000);
                try
                {
                    disposeItem.Clear();
                }
                catch { }
            });
        }
    }

    public void Clear()
    {
        _loadedCmsCount = 0;
        _helmert2dTransformations = new ConcurrentBag<DTOs.Transformations.Helmert2dDTO>();
        _tools.Clear();
        _resourceContainer.Clear();

        var cmsCacheItemsValues = _cmsCacheItems.Values.ToArray();
        _cmsCacheItems.Clear();

        _serviceInitializationTime.Clear();

        foreach (var cmsCacheItem in cmsCacheItemsValues)
        {
            cmsCacheItem.Clear();
        }

        var resourceContainerValues = _resourceContainer.Values.ToArray();
        _resourceContainer.Clear();

        foreach (ToolResource toolResource in resourceContainerValues)
        {
            if (toolResource is IDisposable)
            {
                ((IDisposable)toolResource).Dispose();
            }
        }

        _cacheService.ClearCustomCaches();

        if (_isInitialized)
        {
            // Do not log. If calling task ist finished maybe the Console Handle ist invalid.
            //("Master Cache cleared").LogLine();
        }

        _isInitialized = false;
    }

    public void Log(LogLevel logLevel, string message, params object[] args) => _cacheService.Log(logLevel, message, args);

    private string GetCmsName(string id, string branch)
    {
        var cmsName = (!id.Contains("@"))
            ? String.Empty
            : id.Split('@')[1];

        if (cmsName.Contains("$"))  // remove branch name
        {
            cmsName = cmsName.Substring(0, cmsName.IndexOf("$"));
        }

        if (!String.IsNullOrEmpty(branch))  // append requestet branch name
        {
            cmsName = $"{cmsName}${branch}";
        }

        return cmsName;
    }

    #region Cache Items

    internal CmsCacheItem GetCmsCacheItem(string id, CmsDocument.UserIdentification ui)
    {
        if (id == null)
        {
            return null;
        }

        var cmsName = GetCmsName(id, ui?.Branch);
        CheckForContentAndInitStatic(cmsName);

        if (_cmsCacheItems.ContainsKey(cmsName))
        {
            var cmsCacheItem = _cmsCacheItems[cmsName];

            if (cmsCacheItem.IsCustom == true &&
                ui != null &&
                !ui.UserRolesCmsNames().Contains(cmsName))
            {
                return null;
            }

            if (!cmsCacheItem._isInitialized)
            {
                CmsDocument cms = _cacheService.CmsDocuments.GetCmsDocument(cmsName);
                cmsCacheItem.Init(_cacheService, cmsName, cms, false);
            }

            return cmsCacheItem;
        }
        else if (ui != null && ui.UserRolesCmsNames().Contains(cmsName))
        {
            return LoadCustomCmsCacheItem(cmsName);
        }

        return null;
    }

    private static readonly object _loadCustomCmsLocker = new object();
    internal CmsCacheItem LoadCustomCmsCacheItem(string cmsName)
    {
        _cacheService.Log(LogLevel.Information, "Load custom CMS: {cmsName}", cmsName);
        lock (_loadCustomCmsLocker)
        {
            if (!_cmsCacheItems.ContainsKey(cmsName))
            {
                CmsDocument cms = _cacheService.CmsDocuments.GetCustomCmsDocument(cmsName);
                if (cms != null)
                {
                    var cmsCacheItem = new CmsCacheItem(this);
                    cmsCacheItem.Init(_cacheService, cmsName, cms, true);
                    cmsCacheItem.DisplayName = _cacheService.CmsDocuments.GetCustomCmsDocumentDisplayName(cmsName);
                    _cmsCacheItems.TryAdd(cmsName, cmsCacheItem);
                }
            }
        }

        if (_cmsCacheItems.ContainsKey(cmsName))
        {
            var cmsCacheItem = _cmsCacheItems[cmsName];
            if (!cmsCacheItem._isInitialized)
            {
                CmsDocument cms = _cacheService.CmsDocuments.GetCmsDocument(cmsName);
                cmsCacheItem.Init(_cacheService, cmsName, cms, false);
            }

            _cacheService.Log(LogLevel.Information, "...succeeded (load custom cms)");

            return cmsCacheItem;
        }

        _cacheService.Log(LogLevel.Warning, "custom cms not found: {cmsName}", cmsName);

        return null;
    }

    internal void CheckForContentAndInitStatic(string cmsName)
    {
        if (_cmsCacheItems.Count == 0 || _isCorrupt)
        {
            Clear();
            Init();
        }
        else
        {
            foreach (var cacheItem in _cmsCacheItems.Values.ToArray())   // ThradSafe
            {
                if (!String.IsNullOrWhiteSpace(cmsName) && cacheItem.Name != cmsName)
                {
                    continue;
                }

                if (cacheItem.IsCorrupt && cacheItem.IsCustom == false)
                {
                    if (_cacheService.CmsDocuments.AllCmsDocumentNames().Contains(cacheItem.Name))
                    {
                        var cms = _cacheService.CmsDocuments.GetCmsDocument(cacheItem.Name);

                        cacheItem.Clear();
                        cacheItem.Init(_cacheService, cacheItem.Name, cms, false);
                    }
                }
            }
        }
    }

    #endregion

    #endregion

    #region Classes

    private class ToolResouceManager : IToolResouceManager
    {
        public ToolResouceManager(Type owner, ConcurrentDictionary<string, ToolResource> resourceContainer)
        {
            this.Owner = owner;
            this.ResourceContainer = resourceContainer;
        }

        private Type Owner { get; set; }
        private ConcurrentDictionary<string, ToolResource> ResourceContainer { get; set; }

        public void AddResource(string name, object resource)
        {
            string key = (this.Owner.ToToolId() + "-" + name).Replace(".", "-").ToLower();
            if (resource is IBitmap)
            {
                this.ResourceContainer.TryAdd(key, new ToolImageResouce((IBitmap)resource));
            }
            else
            {
                //this.ResourceContainer.Add(key, new ToolResource(resource));
            }
        }

        public void AddImageResource(string name, IBitmap image)
        {
            AddResource(name, image);
        }

        public void AddImageResource(string name, byte[] image)
        {
            var ms = new MemoryStream(image);
            AddResource(name, Current.Engine.CreateBitmap(ms));
        }
    }

    public class ToolResource
    {
        protected ToolResource()
        {

        }
        public ToolResource(byte[] resourceBytes)
        {
            this.ResourceBytes = resourceBytes;
        }
        public byte[] ResourceBytes { get; set; }
        public string ContentType { get; set; }
    }

    public class ToolImageResouce : ToolResource
    {
        public ToolImageResouce(IBitmap image)
        {
            if (image != null)
            {
                MemoryStream ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                this.ResourceBytes = ms.ToArray();

                this.ContentType = "image/png";
            }
        }
    }

    #endregion
}
