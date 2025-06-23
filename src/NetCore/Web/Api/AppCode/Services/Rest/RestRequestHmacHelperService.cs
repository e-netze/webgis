using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Caching.Services;
using E.Standard.Configuration.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Extensions;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using E.Standard.WebMapping.Core.Abstraction;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestRequestHmacHelperService
{
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly KeyValueCacheService _keyValueCache;
    private readonly MapServiceInitializerService _mapServiceInitializer;
    private readonly ConfigurationService _config;
    private readonly ICryptoService _crypto;
    private readonly IEnumerable<ICustomApiService> _customServices;
    private readonly IRequestContext _requestContext;
    private readonly IEnumerable<ICustomApiSubscriberClientnameService> _customSubscriberClientnameServices = null;

    public RestRequestHmacHelperService(SubscriberDatabaseService subscriberDb,
                                        KeyValueCacheService keyValueCacke,
                                        MapServiceInitializerService mapServiceInitializer,
                                        ConfigurationService config,
                                        ICryptoService crypto,
                                        IRequestContext requestContext,
                                        IEnumerable<ICustomApiService> customServices = null,
                                        IEnumerable<ICustomApiSubscriberClientnameService> customSubscriberClientnameServices = null)
    {
        _subscriberDb = subscriberDb;
        _keyValueCache = keyValueCacke;
        _mapServiceInitializer = mapServiceInitializer;
        _config = config;
        _crypto = crypto;
        _requestContext = requestContext;
        _customServices = customServices;
        _customSubscriberClientnameServices = customSubscriberClientnameServices;
    }

    async public Task<IActionResult> RequestHmac(ApiBaseController controller, string clientid)
    {
        try
        {
            var httpRequest = controller.Request;

            if (String.IsNullOrWhiteSpace(clientid))
            {
                throw new Exception("Unknown client or clientsecret");
            }

            var db = _subscriberDb.CreateInstance();

            string httpRefererCheckSubstituition = String.Empty, clientSecret = String.Empty;
            if (clientid.Contains(":"))
            {
                httpRefererCheckSubstituition = clientid.Split(':')[1];
                clientid = clientid.Split(':')[0];
            }
            else if (clientid.Contains("@"))
            {
                clientSecret = clientid.Split('@')[1];
                clientid = clientid.Split('@')[0];
            }

            var client = db.GetClientByClientId(clientid);
            if (client == null)
            {
                throw new Exception("Unknown client or clientsecret");
            }

            string usernamePrefix = String.Empty, clientName = client.ClientName;

            var customScriberClientname = _customSubscriberClientnameServices.GetCustomClientname(client);
            if (customScriberClientname != null)
            {
                usernamePrefix = customScriberClientname.UsernamePrefix;
                clientName = customScriberClientname.ClientName;
            }
            else
            {
                var subscriber = db.GetSubscriberById(client.Subscriber);
                if (subscriber == null)
                {
                    throw new Exception("Unknown subscriber");
                }

                usernamePrefix = $"{subscriber.Name}@";
            }

            if (!String.IsNullOrWhiteSpace(clientSecret))
            {
                if (client.IsClientSecret(clientSecret))
                {
                    throw new Exception("Unknown client or clientsecret");
                }
            }
            else
            {
                #region Check HttpReferer

                // Übergibt MapBuilder... hier wird natürlich nicht der echte HttpReferer übergeben. Dafür muss ein verschlüsselter Wert mit übergeben werden (=clientid,ticks-utc)
                if (!String.IsNullOrWhiteSpace(httpRefererCheckSubstituition))
                {
                    httpRefererCheckSubstituition = _crypto.DecryptTextDefault(httpRefererCheckSubstituition);
                    if (!httpRefererCheckSubstituition.Contains(","))
                    {
                        throw new ArgumentException("Invalid Http Referer Substitution");
                    }

                    if (httpRefererCheckSubstituition.Split(',')[0] != clientid)
                    {
                        throw new ArgumentException("Invalid Http Referer Substitution (wrong cliendid)");
                    }

                    if (Math.Abs(long.Parse(httpRefererCheckSubstituition.Split(',')[1]) - DateTime.UtcNow.Ticks) > new TimeSpan(0, 0, 30).Ticks)
                    {
                        throw new ArgumentException("Invalid Http Referer Substitution (timestamp)");
                    }
                }
                else
                {
                    string clientReferer = client.ClientReferer;
                    if (string.IsNullOrWhiteSpace(client.ClientReferer))
                    {
                        clientReferer = _config.DefaultHttpReferer() ?? String.Empty;
                    }
                    else
                    {
                        clientReferer += $",{_config.DefaultHttpReferer()}";
                    }
                    string requestReferer = httpRequest.UrlReferrer()?.ToString();
                    if (requestReferer == null)
                    {
                        throw new Exception("Http-Referer: no referer sended");
                    }

                    if (!requestReferer.MatchReferer(clientReferer.Split(',')))
                    {
                        throw new Exception("Http-Referer: '" + requestReferer + "' is an invalid for this client");
                    }
                }

                #endregion
            }

            string username = usernamePrefix + clientName;
            string cacheUsername = username.ToHmacCacheKey();
            int maxChunkSize = _keyValueCache.MaxChunkSize; // 100;

            // Reuse Key
            string publicKey = _keyValueCache.GetCacheValue(cacheUsername);
            var cacheKeys = publicKey.PublicKeyToCacheKeys();
            string userPrincipalString = String.IsNullOrWhiteSpace(publicKey) ? String.Empty :
                                                                                _keyValueCache.GetCacheValues(cacheKeys)
                                                                                              .ConcatenateChunks();

            if (String.IsNullOrWhiteSpace(publicKey) || String.IsNullOrWhiteSpace(userPrincipalString))
            {
                string privateKey = Guid.NewGuid().ToString("N").ToLower();
                var userPrincipalChunks = privateKey.PrivateKeyToUserPrincipalChunks(_crypto,
                                                                                     maxChunkSize,
                                                                                     username,
                                                                                     client.Roles?.ToArray(),
                                                                                     null);

                publicKey = Guid.NewGuid().ToString("N").ToLower().AppendChunkLength(userPrincipalChunks);

                _keyValueCache.Set(cacheUsername,
                                   publicKey);

                _keyValueCache.Set(publicKey.PublicKeyToCacheKeys().ToArray(),
                                   userPrincipalChunks.ToArray());

                userPrincipalString = userPrincipalChunks.ConcatenateChunks();
            }
            else
            {
                #region Check/Update Userroles

                var userPrincipal = userPrincipalString.ToUserPrincipal(_crypto);

                if (client.Roles.EqualContent(userPrincipal.userRoles) == false)
                {
                    var privateKeyChunks = userPrincipal.privateKey.PrivateKeyToUserPrincipalChunks(_crypto,
                                                                                                    maxChunkSize,
                                                                                                    username,
                                                                                                    client.Roles?.ToArray(),
                                                                                                    null);

                    publicKey = publicKey.AppendChunkLength(privateKeyChunks);
                    _keyValueCache.Set(cacheUsername,
                                       publicKey);

                    _keyValueCache.Set(publicKey.PublicKeyToCacheKeys().ToArray(),
                                       privateKeyChunks.ToArray());

                }
                else
                {
                    _keyValueCache.UpdateCacheAside(cacheKeys);
                }

                #endregion
            }


            HmacResponseDTO hmac = new HmacResponseDTO()
            {
                success = true,
                use = true,
                privateKey = userPrincipalString.ToUserPrincipal(_crypto).privateKey,
                publicKey = publicKey,
                ticks = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds,
                username = username
            };

            await _customServices.HandleApiClientAction(clientid, "requesthmac", username);

            return await controller.JsonObject(hmac);
        }
        catch (Exception ex)
        {
            _mapServiceInitializer.LogException(_requestContext, ex, "requesthmac");
            return await controller.ThrowJsonException(ex);
        }
    }
}
