using E.Standard.Caching.Services;
using E.Standard.Configuration.Services;
using E.Standard.Extensions.Compare;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Extensions;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Portal.Core.AppCode.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Portal.Core.AppCode.Services.Authentication;

public class HmacService
{
    private readonly ConfigurationService _config;
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly KeyValueCacheService _keyValueCache;
    private readonly ICryptoService _crypto;

    public HmacService(ConfigurationService config,
                       SubscriberDatabaseService subscriberDb,
                       KeyValueCacheService keyValueCache,
                       ICryptoService crypto)
    {
        _config = config;
        _subscriberDb = subscriberDb;
        _keyValueCache = keyValueCache;
        _crypto = crypto;
    }

    async public Task<HmacResponseDTO> CreateHmacObjectAsync(PortalUser portalUser)
    {
        if (portalUser != null)
        {
            int maxChunkSize = _keyValueCache.MaxChunkSize; // 100;
            string cacheUsername = portalUser?.Username.OrTake("cache:anonymous-user").ToHmacCacheKey();
            string publicKey = _keyValueCache.Get(cacheUsername);

            var cacheKeys = publicKey.PublicKeyToCacheKeys();
            string userPrincipalString = String.IsNullOrWhiteSpace(publicKey)
                            ? String.Empty
                            : _keyValueCache.GetCacheValues(cacheKeys).ConcatenateChunks();

            if (!String.IsNullOrEmpty(userPrincipalString))
            {
                try
                {
                    // check if crypto still possible, otherwise clear cache and crate a new 
                    userPrincipalString.ToUserPrincipal(_crypto);
                }
                catch
                {
                    foreach (var cacheKey in cacheKeys)
                    {
                        _keyValueCache.Remove(cacheKey);
                    }

                    publicKey = String.Empty;
                    userPrincipalString = String.Empty;
                }
            }

            if (String.IsNullOrWhiteSpace(publicKey) || String.IsNullOrWhiteSpace(userPrincipalString))
            {
                string privateKey = Guid.NewGuid().ToString("N").ToLower();
                var userPrincipalChunks = privateKey.PrivateKeyToUserPrincipalChunks(_crypto,
                                                                                     maxChunkSize,
                                                                                     portalUser?.Username,
                                                                                     portalUser?.UserRoles,
                                                                                     portalUser?.RoleParameters);

                publicKey = Guid.NewGuid().ToString("N").ToLower().AppendChunkLength(userPrincipalChunks);

                _keyValueCache.Set(cacheUsername,
                                   publicKey,
                                   portalUser.IsAnonymous ? TimeSpan.FromDays(1000 * 365).TotalSeconds : 0D);

                _keyValueCache.Set(publicKey.PublicKeyToCacheKeys().ToArray(),
                                   userPrincipalChunks.ToArray(),
                                   portalUser.IsAnonymous ? TimeSpan.FromDays(1000 * 365).TotalSeconds : 0D);

                userPrincipalString = userPrincipalChunks.ConcatenateChunks();
            }
            else
            {
                #region Check/Update Userroles

                var userPrincipal = userPrincipalString.ToUserPrincipal(_crypto);

                //Console.WriteLine($"PortalUser Roles :{ String.Join(", ", portalUser.UserRoles ?? new string[0]) }");
                //Console.WriteLine($"UserPrincipal Roles :{ String.Join(", ", userPrincipal.userRoles ?? new string[0]) }");

                if (portalUser != null &&
                    (portalUser.UserRoles.EqualContentWithEmptyAndNullIsEqual(userPrincipal.userRoles) == false ||
                     portalUser.RoleParameters.EqualContentWithEmptyAndNullIsEqual(userPrincipal.roleParametrs) == false))
                {
                    var privateKeyChunks = userPrincipal.privateKey.PrivateKeyToUserPrincipalChunks(_crypto,
                                                                                                    maxChunkSize,
                                                                                                    portalUser.Username,
                                                                                                    portalUser.UserRoles,
                                                                                                    portalUser.RoleParameters);

                    //Console.WriteLine("Update Cachevalues");

                    publicKey = publicKey.AppendChunkLength(privateKeyChunks);
                    _keyValueCache.Set(cacheUsername,
                                       publicKey,
                                       portalUser.IsAnonymous ? TimeSpan.FromDays(1000 * 365).TotalSeconds : 0D);

                    _keyValueCache.Set(publicKey.PublicKeyToCacheKeys().ToArray(),
                                       privateKeyChunks.ToArray(),
                                       portalUser.IsAnonymous ? TimeSpan.FromDays(1000 * 365).TotalSeconds : 0D);

                }
                else
                {
                    _keyValueCache.UpdateCacheAside(cacheKeys);
                }

                #endregion
            }

            int? favStatus = null;
            if (_config.UseFavoriteDetection() && !portalUser.IsAnonymous)
            {
                var subscriberDb = _subscriberDb.CreateInstance();
                favStatus = (int)await subscriberDb.GetFavUserStatusAsync(portalUser.Username);
            }

            return new HmacResponseDTO()
            {
                success = true,
                use = true,
                privateKey = userPrincipalString.ToUserPrincipal(_crypto).privateKey,
                publicKey = publicKey,
                ticks = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds,
                username = portalUser.Username,
                favstatus = favStatus
            };
        }
        else
        {
            return new HmacResponseDTO()
            {
                success = false,
                use = false
            };
        }
    }
}
