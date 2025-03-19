using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using E.Standard.Api.App;
using E.Standard.Api.App.Configuration;
using E.Standard.Api.App.Extensions;
using E.Standard.Caching.Abstraction;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.MessageQueues.Services.Abstraction;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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

    public CacheController(ILogger<CacheController> logger,
                           ConfigurationService config,
                           CacheClearService clearCache,
                           UrlHelperService urlHelper,
                           IMessageQueueService messageQueue,
                           IServiceProvider serviceProvider,
                           ICryptoService cryptoService,
                           IHttpService http,
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
    }

    public IActionResult Index()
    {
        return ViewResult();
    }

    async public Task<IActionResult> Clear(string id = "")
    {
        await _clearCache.ClearCache(id);
        await _messageQueue.EnqueueAsync(
            ApiGlobals.MessageQueuePrefix,
            new string[] { $"cacheclear:{id}" },
            includeOwnQueue: false);

        return await JsonViewSuccess(String.IsNullOrWhiteSpace(_clearCache.LastInitErrorMessage), _clearCache.LastInitErrorMessage);
    }

    [HttpPost]
    async public Task<IActionResult> Upload(string id = "")
    {
        try
        {
            if (!_config.Configuration.IsCmsUploadAllowed(id))
            {
                _logger.LogWarning("CMS-Upload: not allowed/configured");
                return BadRequest("not allowed");
            }


            string username = _config.Configuration.CmsUploadClient(id);

            if (String.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("CMS-Upload: not allowed for user {username}", username);
                return BadRequest("not allowed");
            }

            var jwtTokenService = _serviceProvider.GetRequiredKeyedService<JwtAccessTokenService>($"cms-upload-{id}");

            var authHeader = Request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CMS-Upload: token required");
                return BadRequest("token required");
            }

            var token = authHeader.Substring("bearer ".Length);
            var principal = jwtTokenService.ValidateToken(token);

            if (!username.Equals(principal.Identity.Name, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("CMS-Upload: invalid user/token");
                return BadRequest("invalid user");
            }

            var file = Request.Form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("CMS-Upload: no file uploaded");
                return BadRequest("No file uploaded.");
            }

            using var memoryStream = new MemoryStream();

            await file.CopyToAsync(memoryStream);

            var base64 = _cryptoService.StaticDecrypt(
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

            var path = _config[ApiConfigKeys.ToKey($"cmspath_{id}")];
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

            await _clearCache.ClearCache(id);
            await _messageQueue.EnqueueAsync(
                ApiGlobals.MessageQueuePrefix,
                new string[] { $"cacheclear:{id}" },
                includeOwnQueue: false);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);

            return BadRequest(ex.Message);
        }
    }

    public Task<IActionResult> OgcClear()
    {
        _clearCache.OgcClearCache();

        return JsonViewSuccess(true);
    }

    async public Task<IActionResult> Collect()
    {
        var mem1 = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var mem2 = GC.GetTotalMemory(true) / 1024.0 / 1024.0;

        return await JsonObject(new { succeeded = true, mem1 = mem1, mem2 = mem2 });
    }

    async public Task<IActionResult> List(string pwd)
    {
        var appCachePassword = _config.AppCacheListPassword();

        if (!String.IsNullOrWhiteSpace(appCachePassword) && pwd == appCachePassword)
        {
            var result = new Dictionary<string, object>();

            foreach (var cacheClearableService in _cacheClearableServices)
            {
                result.Add(cacheClearableService.GetType().Name, await cacheClearableService.GetCacheObject());
            }

            return await JsonObject(result);
        }

        return await JsonViewSuccess(false, "not allowed");
    }
}