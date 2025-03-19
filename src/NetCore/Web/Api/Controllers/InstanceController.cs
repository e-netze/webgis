using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.Cryptography.Services;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Logging.Abstraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Api.Core.Controllers;

public class InstanceController : ApiBaseController
{
    private readonly ILogger<InstanceController> _logger;
    private readonly CacheService _cache;
    private readonly ApiConfigurationService _apiConfig;
    private readonly ApiGlobalsService _apiGlobals;
    private readonly IEnumerable<IExpectableUserRoleNamesProvider> _expectableUserRolesNamesProviders;
    private readonly CryptoServiceOptions _cryptoServiceOptions;
    private readonly ApiJavaScriptService _apiJs;
    private readonly IGeoServicePerformanceLogger _geoServicePerformanceLogger;
    private readonly IUsagePerformanceLogger _usagePerformanceLogger;
    private readonly IRequestContext _requestContext;

    public InstanceController(ILogger<InstanceController> logger,
                              UrlHelperService urlHelper,
                              CacheService cache,
                              ApiConfigurationService apiConfig,
                              ApiGlobalsService apiGlobals,
                              IHttpService http,
                              ApiJavaScriptService apiJs,
                              IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                              IOptionsMonitor<CryptoServiceOptions> cryptoServiceOptions,
                              IGeoServicePerformanceLogger geoServicePerformanceLogger,
                              IUsagePerformanceLogger usagePerformanceLogger,
                              IRequestContext requestContext,
                              IEnumerable<ICustomApiService> customServices = null)
        : base(logger, urlHelper, http, customServices)
    {
        _logger = logger;
        _cache = cache;
        _apiConfig = apiConfig;
        _apiGlobals = apiGlobals;
        _apiJs = apiJs;
        _expectableUserRolesNamesProviders = expectableUserRolesNamesProviders;
        _cryptoServiceOptions = cryptoServiceOptions.CurrentValue;
        _geoServicePerformanceLogger = geoServicePerformanceLogger;
        _usagePerformanceLogger = usagePerformanceLogger;
        _requestContext = requestContext;
    }

    public IActionResult Index()
    {
        return ViewResult();
    }

    async public Task<IActionResult> Info()
    {
        if (_cache.CacheInfo.CmsCount == 0)
        {
            _cache.Init(_expectableUserRolesNamesProviders);
        }

        var apiInfo = new ApiInfoDTO()
        {
            JsVersion = _apiJs.GetJsVersion(),
            Cache = _cache.CacheInfo,
            CryptoCompatibilityHash = _cryptoServiceOptions.GenerateHashCode()
        };

        if (Request.Query["f"] == "json" || Request.Query["f"] == "pjson")
        {
            return await JsonObject(apiInfo);
        }

        return ViewResult(apiInfo);
    }

    public IActionResult Logging()
    {
        if (Request.Query["flush"] == "true")
        {
            _requestContext.GetRequiredService<IGeoServicePerformanceLogger>().Flush();
            _requestContext.GetRequiredService<IOgcPerformanceLogger>().Flush();
            _requestContext.GetRequiredService<IUsagePerformanceLogger>().Flush();
            _requestContext.GetRequiredService<IDatalinqPerformanceLogger>().Flush();
        }


        return Json(new
        {
            performed_flush = Request.Query["flush"],
        });
    }

    public IActionResult SecInfo()
    {
        return Json(new ApiSecurityInfo()
        {
            InstanceRoles = _apiConfig.InstanceRoles
        });
    }

    //async public Task<IActionResult> Crash()
    //{
    //    try
    //    {
    //        int div = 0;

    //        var x = 100 / div;
    //        return null;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw ex;
    //    }
    //}
}