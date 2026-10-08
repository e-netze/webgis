using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;

using E.Standard.Api.App;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.Extensions;
using E.Standard.Caching.Abstraction;
using E.Standard.CMS.Core.Branches;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.MessageQueues.Services.Abstraction;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Abstractions;
using E.Standard.WebApp.Options;
using E.Standard.WebApp.Reflection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

namespace Api.Core.Controllers;

public class CacheController : ApiBaseController
{
    private readonly ILogger<CacheController> _logger;
    private readonly ConfigurationService _config;
    private readonly CacheClearService _clearCache;
    private readonly IMessageQueueService _messageQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICryptoService _cryptoService;
    private readonly IEnumerable<ICacheClearableService> _cacheClearableServices;
    private readonly SecurityOptions _securityOptions;

    public CacheController(ILogger<CacheController> logger,
                           ConfigurationService config,
                           CacheClearService clearCache,
                           UrlHelperService urlHelper,
                           IMessageQueueService messageQueue,
                           IServiceProvider serviceProvider,
                           ICryptoService cryptoService,
                           IHttpService http,
                           IOptions<SecurityOptions> securityOptions,
                           IEnumerable<ICacheClearableService> cacheClearableServices,
                           IEnumerable<ICustomApiService> customServices = null)
        : base(logger, urlHelper, http, customServices)
    {
        _logger = logger;
        _config = config;
        _clearCache = clearCache;
        _messageQueue = messageQueue;
        _cacheClearableServices = cacheClearableServices;
        _serviceProvider = serviceProvider;
        _cryptoService = cryptoService;
        _securityOptions = securityOptions.Value;
    }

    public IActionResult Index()
    {
        return ViewResult();
    }

    [EndpointAuthorization(
        AuthorizationType = EndpointAuthorizationType.UrlPassword | EndpointAuthorizationType.Basic | EndpointAuthorizationType.BearerToken,
        AllowIfNotConfigured = true
        )]
    async public Task<IActionResult> Clear(string id = "", string branch = "")
    {
        if (!String.IsNullOrEmpty(branch) && !CmsBranches.IsValidEncoded(branch))
        {
            return await JsonViewSuccess(false, "invalid branch");
        }

        // only reload the branch: {id}${branch}
        var cmsName = String.IsNullOrEmpty(branch)
            ? id
            : CmsBranches.ToCmsName(id ?? String.Empty, branch);

        await _clearCache.ClearCache(cmsName);
        await _messageQueue.EnqueueAsync(
            ApiGlobals.MessageQueuePrefix,
            new string[] { $"cacheclear:{cmsName}" },
            includeOwnQueue: false);

        return await JsonViewSuccess(String.IsNullOrWhiteSpace(_clearCache.LastInitErrorMessage), _clearCache.LastInitErrorMessage);
    }

    [HttpPost]
    async public Task<IActionResult> Upload(string id = "", string branch = "", string branch_name = "", string user = "", string commit = "")
    {
        try
        {
            var authError = AuthorizeCmsUpload(id, requireBranches: !String.IsNullOrEmpty(branch));
            if (authError != null)
            {
                return BadRequest(authError);
            }

            if (!String.IsNullOrEmpty(branch) && !CmsBranches.IsValidEncoded(branch))
            {
                _logger.LogWarning("CMS-Upload: invalid branch {branch}", branch);
                return BadRequest("invalid branch");
            }

            var file = Request.Form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("CMS-Upload: no file uploaded");
                return BadRequest("No file uploaded.");
            }

            using var memoryStream = new MemoryStream();

            await file.CopyToAsync(memoryStream);

            var base64 = _cryptoService.StaticDecrypt_Aes(
                Encoding.UTF8.GetString(memoryStream.ToArray()),
                _config.Configuration.CmsUploadSecret(id));
            var fileBytes = Convert.FromBase64String(base64);

            //byte[] fileBytes = _cryptoService.DecryptBytes(
            //        memoryStream.ToArray(),
            //        _config.Configuration.CmsUploadSecret(id),
            //        useRandomSalt: false
            //    );

            var xml = Encoding.UTF8.GetString(fileBytes);

            var doc = new XmlDocument();
            doc.LoadXml(xml);  // try if xml is correct

            var path = CmsUploadPath(id);

            if (!String.IsNullOrEmpty(branch))
            {
                // branch deploy: {dir}/branches/{branch}/{filename}, no archive
                CmsBranches.WriteBranch(path, new CmsBranchDeployInfo()
                {
                    Branch = String.IsNullOrEmpty(branch_name) ? CmsBranches.TryDecode(branch) : branch_name,
                    EncodedBranch = branch,
                    User = user,
                    Commit = commit,
                    Date = DateTime.UtcNow
                }, branchPath => System.IO.File.WriteAllText(branchPath, xml));

                await ClearCmsCache(CmsBranches.ToCmsName(id, branch));

                return Ok();
            }

            var fi = new FileInfo(path);

            if (fi.Exists)
            {
                var archiveDirectory = new DirectoryInfo(Path.Combine(fi.Directory!.FullName, "_archive"));
                if (!archiveDirectory.Exists)
                {
                    archiveDirectory.Create();
                }

                string archiveFilename = Path.Combine(
                        archiveDirectory.FullName,
                        $"{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}_{fi.Name}");

                fi.CopyTo(archiveFilename);
            }
            else if (fi.Directory.Exists == false)
            {
                fi.Directory.Create();
            }
            System.IO.File.WriteAllText(path, xml);

            await ClearCmsCache(id);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return BadRequest(ex.Message);
        }
    }

    // deployed branches of a cms (same authorization as upload)
    [HttpGet]
    public IActionResult UploadBranches(string id = "")
    {
        try
        {
            var authError = AuthorizeCmsUpload(id, requireBranches: true);
            if (authError != null)
            {
                return BadRequest(authError);
            }

            var infos = CmsBranches.ReadDeployInfos(CmsUploadPath(id));

            return Content(JsonConvert.SerializeObject(infos), "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return BadRequest(ex.Message);
        }
    }

    // remove a deployed branch of a cms (same authorization as upload)
    [HttpPost]
    async public Task<IActionResult> DeleteBranch(string id = "", string branch = "")
    {
        try
        {
            var authError = AuthorizeCmsUpload(id, requireBranches: true);
            if (authError != null)
            {
                return BadRequest(authError);
            }

            if (!CmsBranches.IsValidEncoded(branch))
            {
                return BadRequest("invalid branch");
            }

            CmsBranches.DeleteBranch(CmsUploadPath(id), branch);

            await ClearCmsCache(CmsBranches.ToCmsName(id, branch));

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return BadRequest(ex.Message);
        }
    }

    #region Helper

    // returns an error message or null if authorized
    private string AuthorizeCmsUpload(string id, bool requireBranches)
    {
        if (!_config.Configuration.IsCmsUploadAllowed(id))
        {
            _logger.LogWarning("CMS-Upload: not allowed/configured");
            return "not allowed";
        }

        if (requireBranches && !(bool.TryParse(_config[ApiConfigKeys.AllowBranches], out bool allowBranches) && allowBranches))
        {
            _logger.LogWarning("CMS-Upload: branches not allowed");
            return "branches not allowed";
        }

        string username = _config.Configuration.CmsUploadClient(id);

        if (String.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("CMS-Upload: not allowed for user {username}", username);
            return "not allowed";
        }

        var jwtTokenService = _serviceProvider.GetRequiredKeyedService<JwtAccessTokenService>($"cms-upload-{id}");

        var authHeader = Request.Headers.Authorization.ToString();
        if (!authHeader.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("CMS-Upload: token required");
            return "token required";
        }

        var token = authHeader.Substring("bearer ".Length);
        var principal = jwtTokenService.ValidateToken(token);

        if (!username.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("CMS-Upload: invalid user/token");
            return "invalid user";
        }

        return null;
    }

    private string CmsUploadPath(string id)
        => _config[ApiConfigKeys.ToKey($"cmspath_{id}")]?.Split('|')[0]
           ?? throw new Exception($"cmspath_{id} not configured");

    async private Task ClearCmsCache(string cmsName)
    {
        await _clearCache.ClearCache(cmsName);
        await _messageQueue.EnqueueAsync(
            ApiGlobals.MessageQueuePrefix,
            new string[] { $"cacheclear:{cmsName}" },
            includeOwnQueue: false);
    }

    #endregion
    [EndpointAuthorization(
        AuthorizationType = EndpointAuthorizationType.UrlPassword | EndpointAuthorizationType.Basic | EndpointAuthorizationType.BearerToken,
        AllowIfNotConfigured = true
        )]
    public Task<IActionResult> OgcClear()
    {
        _clearCache.OgcClearCache();

        return JsonViewSuccess(true);
    }

    [EndpointAuthorization(
            AuthorizationType = EndpointAuthorizationType.UrlPassword | EndpointAuthorizationType.Basic | EndpointAuthorizationType.BearerToken
        )]
    async public Task<IActionResult> Collect()
    {
        var mem1 = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var mem2 = GC.GetTotalMemory(true) / 1024.0 / 1024.0;

        return await JsonObject(new { succeeded = true, mem1 = mem1, mem2 = mem2 });
    }

    [EndpointAuthorization(AuthorizationType = EndpointAuthorizationType.UrlPassword | EndpointAuthorizationType.Basic)]
    async public Task<IActionResult> List(string pwd)
    {
        var result = new Dictionary<string, object>();

        foreach (var cacheClearableService in _cacheClearableServices)
        {
            result.Add(cacheClearableService.GetType().Name, await cacheClearableService.GetCacheObject());
        }

        return await JsonObject(result);
    }
}
