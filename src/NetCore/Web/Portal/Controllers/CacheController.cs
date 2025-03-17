using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.MessageQueues.Services.Abstraction;
using E.Standard.Portal.App;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Web.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.WebgisApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class CacheController : PortalBaseController
{
    private readonly ILogger<CacheController> _logger;
    private readonly ConfigurationService _config;
    private readonly UrlHelperService _urlHelper;
    private readonly InMemoryPortalAppCache _cache;
    private readonly IHttpService _http;
    private readonly IMessageQueueService _messageQueue;
    private readonly WebgisApiService _api;

    public CacheController(ILogger<CacheController> logger,
                           ConfigurationService config,
                           UrlHelperService urlHelper,
                           InMemoryPortalAppCache cache,
                           IHttpService http,
                           WebgisApiService api,
                           ICryptoService crypto,
                           IMessageQueueService messageQueue,
                           IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                           IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _config = config;
        _urlHelper = urlHelper;
        _cache = cache;
        _http = http;
        _messageQueue = messageQueue;
        _api = api;
    }

    public IActionResult Index()
    {
        return ViewResult();
    }

    async public Task<IActionResult> Clear(bool clearApi = true)
    {
        try
        {
            _cache.Clear();
            await _messageQueue.EnqueueAsync(
                PortalGlobals.MessageQueuePrefix,
                new string[] { $"cacheclear:" },
                includeOwnQueue: false);

            if (clearApi == true)
            {
                string apiUrl = _urlHelper.ApiInternalUrl(this.Request);  //Request.Url.Scheme + "://" + Viewer4.GetConfigValue("api");
                var _ = await _http.GetStringAsync(
                    $"{apiUrl}/cache/clear",
                    encoding: Encoding.UTF8,
                    timeOutSeconds: 300);
            }
        }
        catch (Exception ex)
        {
            return JsonViewSuccess(false, ex.Message);
        }

        return JsonViewSuccess(true);
    }

    async public Task<IActionResult> List(string pwd)
    {
        var appCachePassword = _config.AppCacheListPassword();

        if (!String.IsNullOrWhiteSpace(appCachePassword) && pwd == appCachePassword)
        {
            // Refresh CmsUserRoles
            await _api.ApiCmsUserRoles(this.Request);

            return JsonObject(new
            {
                users = _cache.GetUserNames()
                    .Select(u => new
                    {
                        name = u,
                        roles = _cache.GetUserRoles(u)
                    }),

                cmsRoles = _cache.AllCmsRoles
            });
        }

        return JsonViewSuccess(false, "not allowed");
    }

    public IActionResult Collect()
    {
        var mem1 = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var mem2 = GC.GetTotalMemory(true) / 1024.0 / 1024.0;

        return JsonObject(new { succeeded = true, mem1 = mem1, mem2 = mem2 });
    }
}