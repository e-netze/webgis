using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using E.Standard.Api.App;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Caching.Services;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Exceptions;
using E.Standard.Web.Abstractions;
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
    private readonly ICryptoService _crypto;
    private readonly KeyValueCacheService _keyValueCache;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly ConfigurationService _config;
    private readonly IEnumerable<IExpectableUserRoleNamesProvider> _expectableUserRolesNamesProviders;

    public SetupController(ILogger<SetupController> logger,
                           CacheService cache,
                           ICryptoService crypto,
                           KeyValueCacheService keyValueCache,
                           SubscriberDatabaseService subscriberDb,
                           UrlHelperService urlHelper,
                           IHttpService http,
                           ConfigurationService config,
                           IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                           IEnumerable<ICustomApiService> customServices = null)
        : base(logger, urlHelper, http, customServices)
    {
        _logger = logger;
        _cache = cache;
        _crypto = crypto;
        _keyValueCache = keyValueCache;
        _subscriberDb = subscriberDb;
        _config = config;
        _expectableUserRolesNamesProviders = expectableUserRolesNamesProviders;
    }

    public IActionResult Index(string token)
    {
        var setupPassword = _config.SecuritySetupPassword();

        if ("forbidden".Equals(setupPassword, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403);
        }

        try
        {
            string decryptedToken = String.IsNullOrEmpty(token)
                ? token
                : _crypto.DecryptTextDefault(token);

            if (token != "{B69E8A3B-B6CE-4E06-841B-6574861D1920}"
               && new Uri(Request.GetDisplayUrl()).Host != "localhost")
            {
                return StatusCode(403);
            }

            var setup = new Setup();
            string setupResponse = setup.Start(_cache, _keyValueCache, _subscriberDb, _expectableUserRolesNamesProviders);

            return PlainView(setupResponse, "text/plain");
        }
        catch (CryptographyException)
        {
            return StatusCode(500);
        }
        catch (Exception)
        {
            throw;
        }
    }
}