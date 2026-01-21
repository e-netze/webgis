#nullable enable

using Api.Core.AppCode.Mvc;
using Api.Core.AppCode.Services;
using E.Standard.Api.App;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Caching.Services;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Extensions.Compare;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.Security.Cryptography.Exceptions;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Api.Core.Controllers;

public class SetupController : ApiBaseController
{
    private readonly ILogger<SetupController> _logger;
    private readonly CacheService _cache;
    private readonly ICryptoService _crypto;
    private readonly KeyValueCacheService _keyValueCache;
    private readonly MigrateKeyValueCacheService _migrateKeyValueCacheService;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly MigrateSubscriberDatabaseService _migrateSubscriberDb;
    private readonly ConfigurationService _config;
    private readonly IEnumerable<IExpectableUserRoleNamesProvider> _expectableUserRolesNamesProviders;

    public SetupController(ILogger<SetupController> logger,
                           CacheService cache,
                           ICryptoService crypto,
                           KeyValueCacheService keyValueCache,
                           MigrateKeyValueCacheService migrateKeyValueCacheService,
                           SubscriberDatabaseService subscriberDb,
                           MigrateSubscriberDatabaseService migrateSubscriberDb,
                           UrlHelperService urlHelper,
                           IHttpService http,
                           ConfigurationService config,
                           IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders,
                           IEnumerable<ICustomApiService>? customServices = null)
        : base(logger, urlHelper, http, customServices)
    {
        _logger = logger;
        _cache = cache;
        _crypto = crypto;
        _keyValueCache = keyValueCache;
        _migrateKeyValueCacheService = migrateKeyValueCacheService;
        _subscriberDb = subscriberDb;
        _migrateSubscriberDb = migrateSubscriberDb;
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

    public IActionResult Migrate()
    {
        StringBuilder migrateResponse = new();
        var setup = new Setup();

        if (_migrateKeyValueCacheService.HasMigrationSettings())
        {
            migrateResponse.Append("Migrate KeyValueCache");
            migrateResponse.Append(Environment.NewLine);
            migrateResponse.Append("###################################################");
            migrateResponse.Append(Environment.NewLine);

            // setup the datebase
            migrateResponse.Append(setup.Start(_cache, _migrateKeyValueCacheService, null, _expectableUserRolesNamesProviders));
            migrateResponse.Append(Environment.NewLine);

            // migrate the keys
            migrateResponse.Append("MIGRATE");
            migrateResponse.Append(Environment.NewLine);
            migrateResponse.Append("###################################################");
            migrateResponse.Append(Environment.NewLine);

            foreach (var key in _keyValueCache.GetAllKeys())
            {
                string val = _keyValueCache.Get(key);
                migrateResponse.Append($"Migriage {key}...");
                _migrateKeyValueCacheService.Set(key, val);
                migrateResponse.Append(Environment.NewLine);
            }

            migrateResponse.Append("Done...");
            migrateResponse.Append(Environment.NewLine);
            migrateResponse.Append("###################################################");
            migrateResponse.Append(Environment.NewLine);

        }

        if (_migrateSubscriberDb.HasMigrationSettings())
        {
            migrateResponse.Append("Migrate SubscriberDatabase");
            migrateResponse.Append(Environment.NewLine);
            migrateResponse.Append("###################################################");
            migrateResponse.Append(Environment.NewLine);

            // setup the datebase
            migrateResponse.Append(setup.Start(_cache, null, _migrateSubscriberDb, _expectableUserRolesNamesProviders));
            migrateResponse.Append(Environment.NewLine);

            // migrate the subscribers
            migrateResponse.Append("MIGRATE");
            migrateResponse.Append(Environment.NewLine);
            migrateResponse.Append("###################################################");
            migrateResponse.Append(Environment.NewLine);

            var sourceSubscriberDb = _subscriberDb.CreateInstance();
            var targetSubscriberDb = _migrateSubscriberDb.CreateInstance();
            if (sourceSubscriberDb is null || targetSubscriberDb is null)
            {
                migrateResponse.Append("Migration SubscriberDatabase instances could not be created.");
            }
            else
            {
                foreach (var subscriber in sourceSubscriberDb.GetSubscribers())
                {
                    if (targetSubscriberDb.GetSubscriberByName(subscriber.Name) is not null)
                    {
                        migrateResponse.Append($"Subscriber {subscriber.Name}({subscriber.Id}) already migrated, skipping...");
                        migrateResponse.Append(Environment.NewLine);

                        continue;
                    }

                    migrateResponse.Append($"Migriage {subscriber.Name}({subscriber.Id})...");
                    targetSubscriberDb.CreateApiSubscriber(subscriber, true);
                    migrateResponse.Append(Environment.NewLine);
                }

                foreach (var apiClient in sourceSubscriberDb.GetAllClients())
                {
                    if (targetSubscriberDb.GetClientByClientId(apiClient.ClientId) is not null)
                    {
                        migrateResponse.Append($"API Client {apiClient.ClientName}({apiClient.Id}) already migrated, skipping...");
                        migrateResponse.Append(Environment.NewLine);

                        continue;
                    }

                    migrateResponse.Append($"Migriage API Client {apiClient.ClientName}({apiClient.Id})...");
                    targetSubscriberDb.CreateApiClient(apiClient, true);
                    migrateResponse.Append(Environment.NewLine);
                }

                migrateResponse.Append("Done...");
                migrateResponse.Append(Environment.NewLine);
                migrateResponse.Append("###################################################");
                migrateResponse.Append(Environment.NewLine);
            }
        }

        return PlainView(migrateResponse.ToString().OrTake("no migration performed"), "text/plain");
    }
}