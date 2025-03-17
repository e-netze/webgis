using E.Standard.Caching.Services;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using Portal.Core.AppCode.Services.WebgisApi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class InstanceController : PortalBaseController
{
    private readonly ILogger<InstanceController> _logger;
    private readonly UrlHelperService _urlHelper;
    private readonly ConfigurationService _config;
    private readonly WebgisApiService _webgisApiService;
    private readonly KeyValueCacheService _keyValueCache;
    private readonly CryptoServiceOptions _cryptoServiceOptions;

    public InstanceController(ILogger<InstanceController> logger,
                              UrlHelperService urlHelper,
                              ConfigurationService config,
                              WebgisApiService webgisApiService,
                              KeyValueCacheService keyValueCache,
                              IOptionsMonitor<ApplicationSecurityConfig> appSecurityConfig,
                              ICryptoService crypto,
                              IOptionsMonitor<CryptoServiceOptions> cryptoServiceOptions,
                              IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _urlHelper = urlHelper;
        _config = config;
        _webgisApiService = webgisApiService;
        _keyValueCache = keyValueCache;
        _cryptoServiceOptions = cryptoServiceOptions.CurrentValue;
    }

    public IActionResult Index()
    {
        return Info().Result;
    }

    async public Task<IActionResult> Info(string pwd = "")
    {
        var info = new PortalInfoDTO()
        {
            PortalVersion = WebGISVersion.Version,
            ApiUrl = _urlHelper.ApiUrl(Request),
            ApiInfo = await _webgisApiService.GetApiInfo(Request),
            CacheType = _keyValueCache.KeyValueCacheType?.ToString() ?? "not available",
            CacheAsideType = _keyValueCache.KeyValueCacheAsideType?.ToString() ?? "not available",
            CryptoCompatibilityHash = _cryptoServiceOptions.GenerateHashCode()
        };

        if (!String.IsNullOrWhiteSpace(pwd) && pwd == _config.AppCacheListPassword())
        {
            info.Headers = new Dictionary<string, string>();

            foreach (var header in this.Request.Headers)
            {
                info.Headers[header.Key] = header.Value;
            }
        }

        if (Request.Query["f"] == "json" || Request.Query["f"] == "pjson")
        {
            return JsonObject(info);
        }

        return ViewResult(info);
    }
}