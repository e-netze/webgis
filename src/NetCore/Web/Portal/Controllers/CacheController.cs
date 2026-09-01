using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.MessageQueues.Services.Abstraction;
using E.Standard.Portal.App;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Abstractions;
using E.Standard.WebApp.Options;
using E.Standard.WebApp.Reflection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.WebgisApi;

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
    private readonly JwtAccessTokenService _tokenService;
    private readonly SecurityOptions _securityOptions;

    public CacheController(ILogger<CacheController> logger,
                           ConfigurationService config,
                           UrlHelperService urlHelper,
                           InMemoryPortalAppCache cache,
                           IHttpService http,
                           WebgisApiService api,
                           ICryptoService crypto,
                           IMessageQueueService messageQueue,
                           JwtAccessTokenService tokenService,
                           IOptions<SecurityOptions> securityOptions,
                           IOptions<ApplicationSecurityConfig> appSecurityConfig,
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
        _tokenService = tokenService;
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
    async public Task<IActionResult> Clear(bool clearApi = false)
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
                string apiUrl = _urlHelper.ApiInternalUrl(this.Request);
                var _ = await _http.GetStringAsync(
                    $"{apiUrl}/cache/clear?token={_tokenService.GenerateToken(_securityOptions.EndpointAuthorizationBearerUsername, 1)}",
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

    [EndpointAuthorization(
            AuthorizationType = EndpointAuthorizationType.UrlPassword | EndpointAuthorizationType.Basic | EndpointAuthorizationType.BearerToken
        )]
    async public Task<IActionResult> List()
    {
        // Refresh CmsUserRoles
        await _api.ApiCmsUserRoles(this.Request);

        var currentUser = this.CurrentPortalUser();

        return JsonObject(new
        {
            currentUser = currentUser is null
                    ? null
                    : new
                    {
                        name = currentUser.Username,
                        displayName = currentUser.DisplayName,
                        roles = currentUser.UserRoles,
                        roleParameters = currentUser.RoleParameters
                    },

            users = _cache.GetUserNames()
                .Select(u => new
                {
                    name = u,
                    roles = _cache.GetUserRoles(u)
                }),

            cmsRoles = _cache.AllCmsRoles
        });
    }

    [EndpointAuthorization(
            AuthorizationType = EndpointAuthorizationType.UrlPassword | EndpointAuthorizationType.Basic | EndpointAuthorizationType.BearerToken
        )]
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
