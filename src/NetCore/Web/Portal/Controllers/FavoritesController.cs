using E.Standard.Caching.Services;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.SubscriberDatabase;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portal.Core.AppCode.Extensions;
using Portal.Core.AppCode.Mvc;
using Portal.Core.AppCode.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Portal.Core.Controllers;

public class FavoritesController : PortalBaseController
{
    private ILogger<FavoritesController> _logger;
    private readonly ConfigurationService _config;
    private readonly UrlHelperService _urlHelper;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly KeyValueCacheService _keyValueCache;

    public FavoritesController(ILogger<FavoritesController> logger,
                               ConfigurationService config,
                               UrlHelperService urlHelper,
                               SubscriberDatabaseService subscriberDb,
                               KeyValueCacheService keyValueCache,
                               ICryptoService crypto,
                               IOptions<ApplicationSecurityConfig> appSecurityConfig,
                               IEnumerable<ICustomPortalSecurityService> customSecurity = null)
        : base(logger, urlHelper, appSecurityConfig, customSecurity, crypto)
    {
        _logger = logger;
        _config = config;
        _urlHelper = urlHelper;
        _subscriberDb = subscriberDb;
        _keyValueCache = keyValueCache;
    }

    public IActionResult ContentMessage()
    {
        string path = $"{_urlHelper.AppRootPath()}/viewercontent/favoriteprogram.txt";

        if (!System.IO.File.Exists(path))
        {
            path = $"{_urlHelper.AppRootPath()}/viewercontent/_favoriteprogram.txt";
        }

        return PlainView(System.IO.File.ReadAllText(path), "text/plain");
    }

    public IActionResult ResetMessage()
    {
        string path = $"{_urlHelper.AppRootPath()}/viewercontent/favoritereset.txt";
        if (!System.IO.File.Exists(path))
        {
            path = $"{_urlHelper.AppRootPath()}/viewercontent/_favoritereset.txt";
        }

        return PlainView(System.IO.File.ReadAllText(path), "text/plain");
    }

    async public Task<IActionResult> Join(bool join)
    {
        try
        {
            if (!_config.UseFavoriteDetection())
            {
                throw new Exception("FavoriteDetection is not enabeled");
            }

            var portalUser = CurrentPortalUser();
            if (portalUser == null || portalUser.IsAnonymous)
            {
                throw new Exception("FavoriteDetection is not allowed for anonymous users");
            }

            var subscriberDb = _subscriberDb.CreateInstance();
            if (!await subscriberDb.SetFavUserStatusAsync(portalUser.Username, join == true ? UserFavoriteStatus.Active : UserFavoriteStatus.Inactive))
            {
                throw new Exception("Can't set users favorite status");
            }

            return JsonViewSuccess(true);
        }
        catch (Exception ex)
        {
            return JsonViewSuccess(false, ex.Message);
        }
    }

    async public Task<IActionResult> Reset()
    {
        try
        {
            string publicKey = this.Request.Query["hmac_pubk"];

            string keyInfoString = _keyValueCache.Get("hmac:" + publicKey);
            if (String.IsNullOrEmpty(keyInfoString))
            {
                throw new Exception("Forbidden: Public-key not valid (" + publicKey + " - Cache:" + _keyValueCache.GetType() + ")");
            }

            string[] keyInfos = keyInfoString.Split('|');
            string privateKey = keyInfos[0];
            string username = keyInfos.Length > 1 ? keyInfos[1] : String.Empty;

            var subscriberDb = _subscriberDb.CreateInstance();

            return JsonViewSuccess(await subscriberDb.DeleteUserFavorites(username, this.Request.Query["hmac_ft"]));
        }
        catch (Exception /*ex*/)
        {
            return JsonViewSuccess(false, "Unknown error");
        }
    }
}