#nullable enable

using Api.Core.AppCode.Extensions;
using E.Standard.Caching.Services;
using E.Standard.CMS.Core;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Security.App.Exceptions;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core.Extensions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Api.Core.AppCode.Services.Authentication;

public class HmacAuthenticationService
{
    private readonly ApiCookieAuthenticationService _cookies;
    private readonly KeyValueCacheService _keyValuleCache;
    private readonly ApiConfigurationService _apiConfig;
    private readonly ICryptoService _crypto;
    private readonly IEnumerable<ICustomUserRolesService>? _customUserRolesServices;

    public HmacAuthenticationService(ApiCookieAuthenticationService cookies,
                                     KeyValueCacheService keyValueCache,
                                     ApiConfigurationService apiConfig,
                                     ICryptoService crypto,
                                     IEnumerable<ICustomUserRolesService>? customUserRolesServices = null)
    {
        _cookies = cookies;
        _keyValuleCache = keyValueCache;
        _apiConfig = apiConfig;
        _crypto = crypto;
        _customUserRolesServices = customUserRolesServices;
    }

    public CmsDocument.UserIdentification GetHmacUser(HttpContext httpContext, bool allowClientIdAndSecret = false)
    {
        var request = httpContext.Request;
        NameValueCollection nvc = request.FormAndQueryParameters();

        CmsDocument.UserIdentification? ui = null;

        try
        {
            if (nvc["hmac"] == "true")
            {
                string publicKey = nvc["hmac_pubk"]!;
                string hmacData = nvc["hmac_data"]!;
                long ticks = long.Parse(nvc["hmac_ts"]!);
                string task = nvc["hmac_ft"]!;
                string branch = _apiConfig.AllowBranches
                    ? nvc["hmac_br"]!
                    : null!;

                DateTime requestTime = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds(ticks);
                TimeSpan ts = DateTime.UtcNow - requestTime;

                if (ts.TotalSeconds > 60)  // use 60, everything else is for testing...
                {
                    throw new Exception("Forbidden: Request expired");
                }

                var hmacVersion = publicKey.HmacVersionFromPublicKey();

                var cacheKeys = publicKey.PublicKeyToCacheKeys(hmacVersion);
                string userPrincipalString = _keyValuleCache.GetValues(cacheKeys)
                                                            .ConcatenateChunks();

                if (String.IsNullOrEmpty(userPrincipalString))
                {
                    throw new NotAuthorizedException($"Forbidden: Public-key not valid ({publicKey} - Cache: {_keyValuleCache.KeyValueCacheType?.ToString()})");
                }

                var userPrincipal = userPrincipalString.ToUserPrincipal(_crypto, hmacVersion);

                string hmacHash = nvc["hmac_hash"]!;
                hmacHash.ValidateHmacHash(userPrincipal.privateKey, hmacData, ticks); // throws Exception

                if (!String.IsNullOrWhiteSpace(userPrincipal.username))
                {
                    ui = new CmsDocument.UserIdentification(userPrincipal.username,
                                                           userPrincipal.userRoles,
                                                           userPrincipal.roleParametrs,
                                                           _apiConfig.InstanceRoles,
                                                           publicKey,
                                                           task: task,
                                                           branch: branch);
                }
            }
            else  // DoTo: sollte das auch in eine eigene Middleware??
            {
                string? username = _cookies.TryGetCookieUsername(httpContext);
                if (username?.StartsWith("clientid:") == true)
                {
                    // clientid:[clientid]:[username]
                    ui = new CmsDocument.UserIdentification(username.Split(':')[2], null, null, null);

                    // ToDo:
                    //ViewData["append-rest-username"] = "User: " + ui.Username;
                }
                else if (username?.StartsWith("subscriber:") == true)
                {
                    ui = new CmsDocument.UserIdentification("subscriber::" + username.Split(':')[2], null, null, null);

                    // ToDo:
                    // ViewData["append-rest-username"] = "User: " + ui.Username;
                }
            }

            if (ui == null)
            {
                ui = CmsDocument.UserIdentification.Anonymous;
            }

            return ui;
        }
        finally
        {
            if (ui != null && _customUserRolesServices != null && _customUserRolesServices.Count() > 0)
            {
                var uiRoles = ui.Userroles != null ? new List<string>(ui.Userroles) : new List<string>();
                foreach (var customUserRolesService in _customUserRolesServices)
                {
                    uiRoles.AddRange(customUserRolesService.CustomUserRoles() ?? new string[0]);
                }
                CmsDocument.UserIdentification.ResetUserroles(ui, uiRoles.Distinct().ToArray());
            }
        }
    }
}
