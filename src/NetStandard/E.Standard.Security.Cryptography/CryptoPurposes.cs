namespace E.Standard.Security.Cryptography;

/// <summary>
/// Purposes for <c>ICryptoService.SignText</c>/<c>EncryptAuthenticated</c> that are shared between Portal and API.
/// </summary>
public static class CryptoPurposes
{
    /// <summary>
    /// Signed "clientid|unix-seconds" the MapBuilder/AppBuilder passes instead of an http referer when requesting a hmac.
    /// </summary>
    public const string HmacRefererSubstitution = "hmac-referer-substitution";

    /// <summary>
    /// Signed "clientid|unix-seconds" the API (subscriber clients page) passes to the portal MapBuilder/AppBuilder
    /// as <c>clientid-sig</c>, to proof that the caller owns the clientid.
    /// </summary>
    public const string BuilderClientId = "builder-clientid";

    /// <summary>
    /// Deterministically encrypted "serviceId|tileUrl" in <c>/tilecache/redirect/{serviceId}/{token}/...</c> urls
    /// (secured WMTS tiles). Deterministic, so the tile urls stay browser-cacheable.
    /// </summary>
    public const string TileCacheRedirect = "tilecache-redirect";
}
