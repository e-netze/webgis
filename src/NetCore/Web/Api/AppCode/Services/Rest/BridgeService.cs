using Api.AppCode.Mvc.Wrapper;
using E.Standard.Api.App;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Collections;
using E.Standard.Extensions.Compare;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core.Services;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Abstraction;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class BridgeService
{
    private readonly HttpContext _httpContext;
    private readonly CacheService _cache;
    private readonly HttpRequestContextService _httpRequestContext;
    private readonly ConfigurationService _config;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly UrlHelperService _urlHelper;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly IRequestContext _requestContext;
    private readonly ICryptoService _crypto;
    private readonly LookupService _lookup;
    private readonly IGlobalisationService _globalisation;
    private readonly string _userLanguage;

    public BridgeService(IHttpContextAccessor httpContextAccessor,
                         CacheService cache,
                         HttpRequestContextService httpRequestContext,
                         ConfigurationService config,
                         SubscriberDatabaseService subscriberDb,
                         UrlHelperService urlHelper,
                         MapServiceInitializerService mapServiceInitializer,
                         IRequestContext requestContext,
                         ICryptoService crypto,
                         LookupService lookup,
                         IGlobalisationService globalisation)
    {
        _httpContext = httpContextAccessor.HttpContext;
        _cache = cache;
        _httpRequestContext = httpRequestContext;
        _config = config;
        _subscriberDb = subscriberDb;
        _urlHelper = urlHelper;
        _mapServiceInitializer = mapServiceInitializer;
        _requestContext = requestContext;
        _crypto = crypto;
        _lookup = lookup;
        _globalisation = globalisation;

        if (_httpContext?.Request != null)
        {
            try
            {
                NameValueCollection nvc = "post".Equals(_httpContext.Request.Method, StringComparison.OrdinalIgnoreCase)
                    ? _httpContext.Request.Form.ToNameValueCollection()
                    : _httpContext.Request.Query.ToNameValueCollection();

                _userLanguage = (nvc["_ul"] ?? _httpContext.Request.Query["_ul"])
                    .OrTake(_globalisation.DefaultLanguage);
            }
            catch { _userLanguage = _globalisation.DefaultLanguage; }
        }
    }

    public Bridge CreateInstance(CmsDocument.UserIdentification ui, Type currentToolType, string storagePath = "")
    {
        var currentTool = Activator.CreateInstance(currentToolType) as IApiButton;

        return CreateInstance(ui, currentTool, storagePath);
    }

    public Bridge CreateInstance(CmsDocument.UserIdentification ui, IApiButton currentTool = null, string storagePath = "")
    {
        return new Bridge(new HttpRequestWrapper(_httpContext.Request),
                            _httpRequestContext,
                            _cache,
                            _config,
                            _subscriberDb,
                            _mapServiceInitializer,
                            _urlHelper,
                            _requestContext,
                            _crypto,
                            _lookup,
                            _globalisation,
                            _userLanguage,
                            ui,
                            currentTool,
                            storagePath);
    }

    public Task<IMapService> TryCreateCustomToolService(CmsDocument.UserIdentification ui, IMap map, string serviceId)
    {
        if (serviceId != null && serviceId.Contains(":"))
        {
            var toolId = serviceId.Split(':')[0];
            var tool = _cache.GetTool(toolId);

            if (tool is IApiCustomToolServiceProvider)
            {
                var bridge = CreateInstance(ui, tool);

                return ((IApiCustomToolServiceProvider)tool).CreateCustomToolService(bridge, map, serviceId);
            }
        }

        return Task.FromResult<IMapService>(null);
    }
}
