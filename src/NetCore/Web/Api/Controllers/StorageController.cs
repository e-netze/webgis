using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using Api.Core.AppCode.Services.Rest;
using Api.Core.Models.Storage;
using E.Standard.Api.App;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.Core.Services;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Core.Controllers;

public class StorageController : ApiBaseController
{
    private readonly ILogger<StorageController> _logger;
    private readonly ApiConfigurationService _apiConfig;
    private readonly BridgeService _bridge;
    private readonly CacheService _cache;
    private readonly ICryptoService _crypto;

    public StorageController(ILogger<StorageController> logger,
                             ApiConfigurationService apiConfig,
                             BridgeService bridge,
                             CacheService cache,
                             UrlHelperService urlHelper,
                             ICryptoService crypto,
                             IHttpService http,
                             IGlobalisationService globalisationService,
                             IEnumerable<ICustomApiService> customServices = null)
        : base(logger, urlHelper, http, customServices, globalisationService)
    {
        _logger = logger;
        _apiConfig = apiConfig;
        _bridge = bridge;
        _cache = cache;
        _crypto = crypto;
    }

    public IActionResult Index()
    {
        return default(IActionResult);
    }

    public Task<IActionResult> SaveData(string name, string data, string type)
    {
        try
        {
            Bridge bridge = GetBridge();

            var blobType = (StorageBlobType)Enum.Parse(typeof(StorageBlobType), GetDecryptedValue(type), true);

            return JsonObject(new StorageResponse(bridge.Storage.Save(
                GetDecryptedValue(name),
                Convert.FromBase64String(GetDecryptedValue(data)),
                blobType)));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> Save(string name, string dataString, string type)
    {
        try
        {
            Bridge bridge = GetBridge();

            var blobType = (StorageBlobType)Enum.Parse(typeof(StorageBlobType), GetDecryptedValue(type), true);

            return JsonObject(new StorageResponse(bridge.Storage.Save(
                GetDecryptedValue(name),
                GetDecryptedValue(dataString),
                blobType)));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> Exists(string name, string type)
    {
        try
        {
            Bridge bridge = GetBridge();

            var blobType = (StorageBlobType)Enum.Parse(typeof(StorageBlobType), GetDecryptedValue(type), true);

            return JsonObject(new StorageResponse(bridge.Storage.Exists(
                GetDecryptedValue(name), blobType)));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> Load(string name, string type)
    {
        try
        {
            Bridge bridge = GetBridge();

            var blobType = (StorageBlobType)Enum.Parse(typeof(StorageBlobType), GetDecryptedValue(type), true);

            return JsonObject(new StorageResponse(bridge.Storage.Load(
                GetDecryptedValue(name), blobType)));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> LoadString(string name, string type)
    {
        try
        {
            Bridge bridge = GetBridge();

            var blobType = (StorageBlobType)Enum.Parse(typeof(StorageBlobType), GetDecryptedValue(type), true);

            return JsonObject(new StorageResponse(bridge.Storage.LoadString(
                GetDecryptedValue(name), blobType)));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> LoadStrings(string names, string type)
    {
        try
        {
            Bridge bridge = GetBridge();

            var blobType = (StorageBlobType)Enum.Parse(typeof(StorageBlobType), GetDecryptedValue(type), true);

            return JsonObject(new StorageResponse(bridge.Storage.LoadStrings(
                GetDecryptedValue(names).Split(','), blobType)));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> Remove(string name, string type, string recursive)
    {
        try
        {
            Bridge bridge = GetBridge();

            var blobType = (StorageBlobType)Enum.Parse(typeof(StorageBlobType), GetDecryptedValue(type), true);
            bool removeRecursive = recursive != null && GetDecryptedValue(recursive).ToLower() == "true";

            return JsonObject(new StorageResponse(bridge.Storage.Remove(
                GetDecryptedValue(name), blobType, removeRecursive)));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> GetNames()
    {
        try
        {
            Bridge bridge = GetBridge();

            return JsonObject(new StorageResponse(bridge.Storage.GetNames()));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> GetAllNames()
    {
        try
        {
            Bridge bridge = GetBridge();

            return JsonObject(new StorageResponse(bridge.Storage.GetAllNames()));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> GetDirectoryNames(string subDir = "", string filter = "")
    {
        try
        {
            Bridge bridge = GetBridge();

            return JsonObject(new StorageResponse(bridge.Storage.GetDirectoryNames(
                GetDecryptedValue(subDir),
                GetDecryptedValue(filter))));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> CreateEncryptedName(string user, string name)
    {
        try
        {
            Bridge bridge = GetBridge(false);

            return JsonObject(new StorageResponse(bridge.Storage.CreateEncryptedName(
                GetDecryptedValue(user),
                GetDecryptedValue(name))));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> AppendToName(string name, string appendix)
    {
        try
        {
            Bridge bridge = GetBridge(false);

            return JsonObject(new StorageResponse(bridge.Storage.AppendToName(
                GetDecryptedValue(name),
                GetDecryptedValue(appendix))));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> DecryptName(string name)
    {
        try
        {
            Bridge bridge = GetBridge(false);

            return JsonObject(new StorageResponse(bridge.Storage.DecryptName(
                GetDecryptedValue(name))));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> SetUniqueIndexItem(string indexName, string key, string val)
    {
        try
        {
            Bridge bridge = GetBridge();

            return JsonObject(new StorageResponse(bridge.Storage.SetUniqueIndexItem(
                GetDecryptedValue(indexName),
                GetDecryptedValue(key),
                GetDecryptedValue(val))));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> GetUniqueIndexItem(string indexName, string key)
    {
        try
        {
            Bridge bridge = GetBridge();

            return JsonObject(new StorageResponse(bridge.Storage.GetUniqueIndexItem(
                GetDecryptedValue(indexName),
                GetDecryptedValue(key))));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> GetUniqueIndexKeys(string indexName)
    {
        try
        {
            Bridge bridge = GetBridge();

            return JsonObject(new StorageResponse(bridge.Storage.GetUniqueIndexKeys(
                GetDecryptedValue(indexName))));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    public Task<IActionResult> RemoveUniqueIndexItem(string indexName, string key)
    {
        try
        {
            Bridge bridge = GetBridge();

            return JsonObject(new StorageResponse(bridge.Storage.RemoveUniqueIndexItem(
                GetDecryptedValue(indexName),
                GetDecryptedValue(key))));
        }
        catch (Exception ex)
        {
            return ThrowJsonException(ex);
        }
    }

    #region Helper

    private Bridge GetBridge(bool toolRequired = true)
    {
        CmsDocument.UserIdentification ui = new CmsDocument.UserIdentification(
            GetDecryptedRequestParameter("__un"),
            String.IsNullOrWhiteSpace(GetDecryptedRequestParameter("__ur")) ? null : GetDecryptedRequestParameter("__ur").Split(','),
            String.IsNullOrWhiteSpace(GetDecryptedRequestParameter("__urp")) ? null : GetDecryptedRequestParameter("__urp").Split(','),
            _apiConfig.InstanceRoles
            );

        IApiButton currentTool = _cache.GetTool(GetDecryptedRequestParameter("__tid"));
        if (toolRequired && currentTool == null)
        {
            throw new Exception("Can't determine current tool");
        }

        var bridge = _bridge.CreateInstance(ui,
                                            currentTool,
                                            _apiConfig.StorageRootPath2);

        string rawEventString = GetDecryptedRequestParameter("__res");
        if (rawEventString != null)
        {
            bridge.CurrentEventArguments = new ApiToolEventArguments(rawEventString);
        }

        return bridge;
    }

    private string GetDecryptedRequestParameter(string parameterName)
    {
        string val =
            Request.HasFormContentType && !String.IsNullOrEmpty(Request.Form[parameterName]) ?
            Request.Form[parameterName] : Request.Query[parameterName];

        return GetDecryptedValue(val);
    }

    private string GetDecryptedValue(string val)
    {
        if (String.IsNullOrWhiteSpace(val))
        {
            return String.Empty;
        }

        return _crypto.DecryptTextDefault(val);
    }

    #endregion
}