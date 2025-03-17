using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using E.Standard.Api.App;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Caching.Services;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.Core.Services;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Api.Core.Controllers;

public class SetupController : ApiBaseController
{
    private readonly ILogger<SetupController> _logger;
    private readonly CacheService _cache;
    private readonly KeyValueCacheService _keyValueCache;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly ConfigurationService _config;
    private readonly IEnumerable<IExpectableUserRoleNamesProvider> _expectableUserRolesNamesProviders;

    public SetupController(ILogger<SetupController> logger,
                           CacheService cache,
                           KeyValueCacheService keyValueCache,
                           SubscriberDatabaseService subscriberDb,
                           UrlHelperService urlHelper,
                           IHttpService http,
                           ConfigurationService config,
                           IGlobalisationService globalisationService,
                           IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                           IEnumerable<ICustomApiService> customServices = null)
        : base(logger, urlHelper, http, customServices, globalisationService)
    {
        _logger = logger;
        _cache = cache;
        _keyValueCache = keyValueCache;
        _subscriberDb = subscriberDb;
        _config = config;
        _expectableUserRolesNamesProviders = expectableUserRolesNamesProviders;
    }

    public IActionResult Index(string pwd)
    {
        var setupPassword = _config.SecuritySetupPassword();

        if ("forbidden".Equals(setupPassword, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403);
        }

        if ((!String.IsNullOrEmpty(setupPassword) && setupPassword == pwd) ||
            new Uri(Request.GetDisplayUrl()).Host == "localhost")
        {

            var setup = new Setup();
            string setupResponse = setup.Start(_cache, _keyValueCache, _subscriberDb, _expectableUserRolesNamesProviders);

            return PlainView(setupResponse, "text/plain");
        }

        return StatusCode(403);
    }
}